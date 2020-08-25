# Cross-platform benchmarks

TBD

## Tests

### File access

Read as many small files as possible in 30 seconds
- Listing all the files in a folder and reading them sequentially
- Listing all the files in a folder and reading them using promises
- Use streams to read the first 1KB
- Content needs to get to the process running the UI web content (should messages be batched?)

## Running the tests

1. Install prerequesites for all platforms
2. Prepare the tests running `node prepare.js`
