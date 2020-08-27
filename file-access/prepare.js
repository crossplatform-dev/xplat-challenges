const { join } = require('path');
const fs = require('fs');
const { promisify } = require('util');

const readAsync = promisify(fs.readFile);
const writeAsync = promisify(fs.writeFile);
const mkdirAsync = promisify(fs.mkdir);

const FILES = 10000;
let fileCount = 0;
const TOTAL_WORKERS = 100;

const writeFile = async (content, target) => {
    const position = fileCount--;

    if (position <= 0) {
        return;
    }

    if (position % 100 === 0) {
        console.log(position);
    }

    const filename = join(target, `${position}`);

    await writeAsync(filename, content, 'utf-8');

    return writeFile(content, target);
};

const prepareFiles = async (filename, targetFolder) => {
    const content = await readAsync(join(__dirname, '..', 'source-files', filename), 'utf-8');

    const workers = [];

    const fixturesPath = join(__dirname, 'fixtures');
    const target = join(__dirname, 'fixtures', targetFolder);

    try {
        await mkdirAsync(fixturesPath);
    } catch{ }

    try {
        await mkdirAsync(target);
    } catch{ }

    for (let i = 0; i < TOTAL_WORKERS; i++) {
        workers.push(writeFile(content, target));
    }

    await Promise.all(workers);
};

const prepare = async () => {
    fileCount = FILES;
    await prepareFiles('4k.txt', '4k');
    fileCount = FILES;
    await prepareFiles('1mb.txt', '1mb');
};

prepare();