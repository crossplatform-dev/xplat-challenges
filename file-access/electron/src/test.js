const fs = require('fs');
const { join } = require('path')
const util = require('util');

const readAsync = util.promisify(fs.readFile);
const writeAsync = util.promisify(fs.writeFile);
const readdirAsync = util.promisify(fs.readdir);

const RESULTS_FILE = `results.csv`;
const PARALLEL_WORKERS = 100;
const RERUNS = 5;

const readFolderContent = async (folderName) => {
    const start = Date.now();
    const sourceFolder = join(process.cwd(), '..', 'fixtures', folderName);
    const files = (await readdirAsync(sourceFolder)).map((file) => {
        return join(sourceFolder, file);
    });
    const end = Date.now();

    const time = end - start;

    return [files, time];
};

const readSequentially = async (files) => {
    const start = Date.now();

    for (let i = 0; i < files.length; i++) {
        await readAsync(files[i], 'utf-8');
    }

    const end = Date.now();
    const time = end - start;

    return [files.length, time];
}

const readParallel = async (files) => {
    if (files.length <= 0) {
        return;
    }

    const file = files.pop();

    await readAsync(file, 'utf-8');

    return readParallel(files);
};

const readInParallel = async (files) => {
    const totalFiles = files.length;
    const start = Date.now();

    const workers = [];

    for (let i = 0; i < PARALLEL_WORKERS; i++) {
        workers.push(readParallel(files));
    }

    await Promise.all(workers);

    const end = Date.now();
    const time = end - start;

    return [totalFiles, time];
};


const benchmark = async (fileSize) => {
    const [files, timeToList] = await readFolderContent(fileSize);
    const [, seqTime] = await readSequentially(files);
    const [, parallelTime] = await readInParallel(files);

    return [timeToList, seqTime, parallelTime];
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
    const fileSizes = ['4k', '1mb'];

    while (fileSizes.length > 0) {
        const size = fileSizes.shift();
        const results = [];

        for (let i = 0; i < RERUNS; i++) {
            document.getElementById('status').textContent = `Running benchmark for size ${size} (${i})`;
            const result = await benchmark(size);

            results.push(result);
        }

        const [timeToList, seqTime, parallelTime] = calculateAverage(results);

        await writeAsync(join(process.cwd(), '..',`electron-${size}-${RESULTS_FILE}`),
            `Action,Time elapsed (ms)
Read dir,${timeToList}
Sequential read,${seqTime}
Parallel read,${parallelTime}
`, 'utf-8');
    }

    document.getElementById('status').textContent = `Done`;
};

document.getElementById('start').addEventListener('click', run);
