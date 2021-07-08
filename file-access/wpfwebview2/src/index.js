const PARALLEL_WORKERS = 100;
const RERUNS = 5;
const FILES = 1000;

const callbacks = new Map();

const generateMessageId = () => {
    const id = Math.floor(Math.random() * 10000000);

    if (callbacks.has(id)) {
        return generateMessageId();
    }

    return id;
};

const storeCallback = (id, callback, cbks) => {
    cbks.set(id, callback);
};

const sendAction = (action, info, callback) => {
    const id = generateMessageId();

    const message = {
        action,
        info,
        id
    };

    if (callback) {
        storeCallback(id, callback, callbacks);
    }

    window.chrome.webview.postMessage(message);
};

/** Handles the messages sent to WebView from C# */
const onMessage = (evt) => {
    const message = evt.data;

    const callback = callbacks.get(message.id);
    callbacks.delete(message.id);
    let payload = message.info;
    try {
        payload = JSON.parse(message.info)
    } catch{ }

    callback(payload);
};

const deleteFixtures = async () => {
    const start = Date.now();

    return new Promise((resolve) => {

        sendAction('deleteFixtures', "", () => {
            const end = Date.now();
            const time = end - start;

            resolve(time);
        });
    });
};

const createFolder = async (folder) => {
    const start = Date.now();

    return new Promise((resolve) => {

        sendAction('createFolder', folder, () => {
            const end = Date.now();
            const time = end - start;

            resolve(time);
        });
    });
};


const readFolderContent = (folderName) => {
    const start = Date.now();

    return new Promise((resolve) => {

        sendAction('readFolder', folderName, (files) => {
            const end = Date.now();
            const time = end - start;

            resolve([files, time]);
        });
    });
};

const readFile = (file) => {
    return new Promise((resolve) => {
        sendAction('readFile', file, (content) => {
            resolve(content);
        });
    });
};

const readSequentially = async (files) => {
    const start = Date.now();

    for (let i = 0; i < files.length; i++) {
        await readFile(files[i]);
    }

    const end = Date.now();
    const time = end - start;

    return [files.length, time];
}

const readParallel = async (files) => {
    if (files.length <= 0) {
        return;
    }

    if (files.length % 100 === 0) {
        console.log(files.length);
    }

    const file = files.pop();

    await readFile(file);

    return readParallel(files);
};

const readInParallel = async (files) => {
    const start = Date.now();
    const totalFiles = files.length;

    const workers = [];

    for (let i = 0; i < PARALLEL_WORKERS; i++) {
        workers.push(readParallel(files));
    }

    await Promise.all(workers);

    const end = Date.now();
    const time = end - start;

    return [totalFiles, time];
};

const writeAsync = (file, content) => {
    return new Promise((resolve) => {
        sendAction('writeFile', JSON.stringify({ path: file, content: content }), (content) => {
            resolve(content);
        });
    });
};

const writeFile = async (files, content) => {
    if (files.length <= 0) {
        return;
    }

    const filename = files.pop();

    await writeAsync(filename, content, 'utf-8');

    return writeFile(files, content);
};

/**
 * @param {string[]} files
 * @param {string} content 
 */
const writeConcurrently = async (files, content) => {
    const totalFiles = files.length;
    const start = Date.now();

    const workers = [];

    for (let i = 0; i < PARALLEL_WORKERS; i++) {
        workers.push(writeFile(files, content));
    }

    await Promise.all(workers);

    const end = Date.now();
    const time = end - start;

    return [totalFiles, time];
};

/**
 * Creates the files used for reading later concurrently
 * @param {string} folderName 
 */
const writeFiles = async (folderName) => {
    const start = Date.now();

    const targetFolder = `fixtures\\${folderName}`;
   
    await createFolder(targetFolder);
    
    const files = [];

    for (let i = 0; i < FILES; i++) {
        files.push(`${targetFolder}\\${folderName}-${i}.txt`);
    }

    const content = await readFile(`source-files\\${folderName}.txt`);

    await writeConcurrently(files, content);

    const end = Date.now();

    const time = end - start;

    return [files, time];
};



const benchmark = async (fileSize) => {
    await deleteFixtures();

    const [, timeToWrite] = await writeFiles(fileSize);
    const [files, timeToList] = await readFolderContent(fileSize);
    const [, seqTime] = await readSequentially(files);
    const [, parallelTime] = await readInParallel(files);
     
    return [timeToWrite, timeToList, seqTime, parallelTime];
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

const writeResults = (results, size) => {
    return new Promise((resolve) => {
        sendAction('writeResults', JSON.stringify({ results, size }), () => {
            resolve();
        });
    });
};

const run = async () => {

    const fileSizes = ['4k', '1mb'];

    while (fileSizes.length > 0) {
        const size = fileSizes.shift();
        const results = [];

        for (let i = 0; i < RERUNS; i++) {
            document.getElementById('status').textContent = `Running benchmark for size ${size} (${i})`;
            const result = await benchmark(size);

            results.push(result);
        }

        const [timeToWrite, timeToList, seqTime, parallelTime] = calculateAverage(results);

        const message =
            `Action,Time elapsed (ms)
Write files,${timeToWrite}
Read dir,${timeToList}
Sequential read,${seqTime}
Parallel read,${parallelTime}
`;

        await writeResults(message, size);
    }

    document.getElementById('status').textContent = `Done`;
};

window.chrome.webview.addEventListener('message', onMessage);
document.getElementById('start').addEventListener('click', run);
