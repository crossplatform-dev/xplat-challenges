//@ts-check

const RESULTS_FILE = `results.csv`;
const PARALLEL_WORKERS = 10;
const RERUNS = 1;
// const ROOT = join(process.cwd(), '..');
const FIXTURES_PATH = 'fixtures';
const SOURCES_PATH = 'source-files';
const FILES = 10000;
const folderHandles = new Map();


/**
 * The user needs to grant access to the top directory to get access to everything
 * and not have to ask for permission at every single step.
 *
 * Read https://web.dev/file-system-access/ for more information
 */

/**
 *
 * @param {FileSystemDirectoryHandle} parentHandle
 * @param {string} fileName
 */
const getFileHandle = async (parentHandle, fileName, options) => {
    let directoryHandle = parentHandle;
    let parts = fileName.split('/');
    let realFileName = parts.pop();
    const path = parts.join('/');

    if (folderHandles.has(path)) {
        directoryHandle = folderHandles.get(path);
    } else {

        while (parts.length > 0) {
            directoryHandle = await directoryHandle.getDirectoryHandle(parts.shift());
        }

        folderHandles.set(path, directoryHandle);
    }

    const fileHandle = await directoryHandle.getFileHandle(realFileName, options);

    return fileHandle;
};

/**
 * Returns the text contents of a file.
 * @param {FileSystemDirectoryHandle} parentHandle
 * @param {string} fileName
 */
const readAsync = async (parentHandle, fileName) => {
    try {
        const fileHandle = await getFileHandle(parentHandle, fileName);
        const file = await fileHandle.getFile();

        return file.text();
    } catch (e) {
        console.error(`readAsync (${fileName}): ${e.message}`);
        throw e;
    }
};

const writtenFiles = new Set();

/**
 * Creates a new file `fileName` with `content` into the given directory handle `directoryHandle`
 * @param {FileSystemDirectoryHandle} directoryHandle
 * @param {string} fileName
 * @param {string} content
 */
const writeAsync = async (directoryHandle, fileName, content) => {
    const fileHandle = await getFileHandle(directoryHandle, fileName, { create: true });
    const writable = await fileHandle.createWritable();

    // Write the contents of the file to the stream.
    await writable.write(content);
    // Close the file and write the contents to disk.
    await writable.close();
};

/**
 * Opens the directory picker and returns the contents of that directory
 * @param {FileSystemDirectoryHandle} directoryHandle
 * @param {string} subdirectory
 * @returns {Promise<[FileSystemDirectoryHandle, AsyncIterableIterator<FileSystemHandle>]>}
 */
const readdirAsync = async (directoryHandle, subdirectory) => {
    try {
        let folderHandler = directoryHandle;

        if (folderHandles.has(subdirectory)) {
            folderHandler = folderHandles.get(subdirectory);
        } else {
            const parts = subdirectory.split('/');

            while (parts.length > 0) {
                folderHandler = await folderHandler.getDirectoryHandle(parts.shift());
            }

            folderHandles.set(subdirectory, folderHandler);
        }

        return [folderHandler, folderHandler.values()];
    } catch (e) {
        console.error(`readdirAsync: ${e.message}`);
        throw e;
    }
};

/**
 * Creates a directory `dirName` into the given directory `directoryHandle`.
 * @param {FileSystemDirectoryHandle} directoryHandle
 * @param {string} dirName
 */
const mkdirAsync = (directoryHandle, dirName) => {
    return directoryHandle.getDirectoryHandle(dirName, {
        create: true
    });
}

/**
 * Selects the parent directory to perform all operations
 * @returns {Promise<FileSystemDirectoryHandle>}
 */
const getParentDirectory = async () => {
    const dirHandle = await window.showDirectoryPicker();
    return dirHandle;
};

/**
 * Recursively deletes the contents of the directory `dirName` in `directoryHandle`
 * @param {FileSystemDirectoryHandle} directoryHandle
 * @param {string} dirName
 */
const del = async (directoryHandle, dirName) => {
    // Recursively delete a folder.
    await directoryHandle.removeEntry(dirName, { recursive: true });
};


/**
 *
 * @param {FileSystemDirectoryHandle} parentDirectoryHandle
 * @param {string} folderName
 * @returns {Promise<[string[], number]>}
 */
const readFolderContent = async (parentDirectoryHandle, folderName) => {
    const start = Date.now();
    const [sourceFolder, handles] = await readdirAsync(parentDirectoryHandle, folderName);
    const time = Date.now() - start;
    const files = [];

    for await (const entry of handles) {
        if (entry.kind === 'file') {
            files.push(`${folderName}/${entry.name}`);
        }
    }

    return [files, time];
};

/**
 * @param {FileSystemDirectoryHandle} parentDirectoryHandle
 * @param {string[]} files
 */
const readSequentially = async (parentDirectoryHandle, files) => {
    const start = Date.now();
    const contents = new Map();

    for (const file of files) {
        const content = await readAsync(parentDirectoryHandle, file);
        contents.set(file, content);
    }

    const end = Date.now();
    const time = end - start;

    return [files.length, time];
};

/**
 * @param {FileSystemDirectoryHandle} parentDirectoryHandle
 * @param {string[]} files
 */
const read = async (parentDirectoryHandle, files) => {
    if (files.length <= 0) {
        return;
    }

    const file = files.pop();

    await readAsync(parentDirectoryHandle, file);

    return read(parentDirectoryHandle, files);
};

/**
 * @param {FileSystemDirectoryHandle} parentDirectoryHandle
 * @param {string[]} files
 */
const readConcurrently = async (parentDirectoryHandle, files) => {
    const start = Date.now();
    const workers = [];
    // const handleArray = [];
    const totalFiles = files.length;

    for (let i = 0; i < PARALLEL_WORKERS; i++) {
        workers.push(read(parentDirectoryHandle, files));
    }

    await Promise.all(workers);

    const end = Date.now();
    const time = end - start;

    return [totalFiles, time];
};

/**
 * @param {FileSystemDirectoryHandle} parentDirectoryHandle
 * @param {string} content
 * @param {string[]} files
 */
const writeFile = async (parentDirectoryHandle, files, content) => {
    if (files.length <= 0) {
        console.log('Worker done');
        return;
    }

    while (files.length > 0) {
        const filename = files.shift();
        try {
            await writeAsync(parentDirectoryHandle, filename, content);
        } catch (e) {
            console.error(`Error writting ${filename} (will retry)`);
            files.push(filename)
        }
    }
};

/**
 * @param {FileSystemDirectoryHandle} parentDirectoryHandle
 * @param {string[]} files
 * @param {string} content
 */
const writeConcurrently = async (parentDirectoryHandle, files, content) => {
    const totalFiles = files.length;
    const start = Date.now();

    const workers = [];

    const chunks = [];
    for (let i = 0; i < FILES; i++) {
        const workerNumber = i % PARALLEL_WORKERS;

        if (!chunks[workerNumber]) {
            chunks[workerNumber] = [];
        }
        chunks[workerNumber].push(files[i]);
    }

    for (let i = 0; i < chunks.length; i++) {
        workers.push(writeFile(parentDirectoryHandle, chunks[i], content));
    }

    await Promise.all(workers);

    const end = Date.now();
    const time = end - start;

    return [totalFiles, time];
};

/**
 * Creates the files used for reading later concurrently
 * @param {FileSystemDirectoryHandle} parentDirectoryHandle
 * @param {string} folderName
 * @param {string} content
 * @returns {Promise<[string[], number]>}
 */
const writeFiles = async (parentDirectoryHandle, folderName, content) => {
    const start = Date.now();
    const parts = folderName.split('/');
    let folderHandler;

    if (folderHandles.has(folderName)) {
        folderHandler = folderHandles.get(folderName);
    } else {
        folderHandler = parentDirectoryHandle;

        // Create all the required paths
        while (parts.length > 0) {
            folderHandler = await mkdirAsync(folderHandler, parts.shift());
        }
        folderHandles.set(folderName, folderHandler);
    }

    // Write file contents
    const files = [];

    for (let i = 0; i < FILES; i++) {
        files.push(`${folderName}/${i}.txt`);
    }

    await writeConcurrently(parentDirectoryHandle, files, content);

    const end = Date.now();

    const time = end - start;

    return [files, time];
};


/**
 *
 * @param {FileSystemDirectoryHandle} parentDirectoryHandle
 * @param {string} fileSize
//  * @returns {Promise<[number,number,number,number]>}
 */
const benchmark = async (parentDirectoryHandle, fileSize) => {
    const start = Date.now();
    // TODO: This process is painfully slow on the browser

    // await del(parentDirectoryHandle, FIXTURES_PATH);
    // const endDel = Date.now();

    // console.log(`Deletion completed in ${endDel - start}`);

    // const content = await readAsync(parentDirectoryHandle, `${SOURCES_PATH}/${fileSize}.txt`);

    const targetPath = `${FIXTURES_PATH}/${fileSize}`;

    // const [, timeToWrite] = await writeFiles(parentDirectoryHandle, targetPath, content);

    // const endWrite = Date.now();
    // console.log(`Fixtures created in ${endWrite - endDel}`);

    // Read files in different modes
    const [files, timeToList] = await readFolderContent(parentDirectoryHandle, targetPath);
    const endListContents = Date.now();
    console.log(`Contents read in ${endListContents - start}`);

    const [, seqTime] = await readSequentially(parentDirectoryHandle, files);
    const endSequentialRead = Date.now();
    console.log(`Sequential read done in ${endSequentialRead - endListContents}`);

    const [, parallelTime] = await readConcurrently(parentDirectoryHandle, files);
    const endConcurrentRead = Date.now();
    console.log(`Concurrent read done in ${endConcurrentRead - endSequentialRead}`);

    return [-1, timeToList, seqTime, parallelTime];
};

/**
 * Results is an array of arrays:
 * [[t1,t2,t3, ...], [t1, t2, t3, ...], ...]
 *
 * Need to calculate the average of each t1, t2, t3...
 */
const calculateAverage = (results) => {

    const total = results.reduce((sum, result) => {
        for (let i = 0; i < result.length; i++) {
            if (isNaN(sum[i])) {
                sum[i] = 0;
            }

            sum[i] += result[i];
        }
        return sum;
    }, []);

    const final = total.map((value) => {
        return value / results.length;
    });

    return final;
};

const run = async () => {
    // document.getElementById('start').setAttribute('disabled', 'disabled');
    const parentDirectoryHandle = await getParentDirectory();
    const fileSizes = ['4k', '1mb'];

    while (fileSizes.length > 0) {
        const size = fileSizes.shift();
        const results = [];

        for (let i = 0; i < RERUNS; i++) {
            document.getElementById('status').textContent = `Running benchmark for size ${size}(${i})`;
            const result = await benchmark(parentDirectoryHandle, size);

            results.push(result);
        }

        const [timeToWrite, timeToList, seqTime, parallelTime] = calculateAverage(results);

        //     await writeAsync(join(process.cwd(), '..', `electron - ${size} -${RESULTS_FILE} `),
        //         `Action, Time elapsed(ms)
        // Write files, ${timeToWrite}
        // Read dir, ${timeToList}
        // Sequential read, ${seqTime}
        // Parallel read, ${parallelTime}
        // `, 'utf-8');
    }

    document.getElementById('start').removeAttribute('disabled');

    document.getElementById('status').textContent = `Done`;
};

document.getElementById('start').addEventListener('click', run);
