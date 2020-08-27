# Cross-platform benchmarks

TBD

## File access

1. Install prerequesites for all platforms (node, yarn, Visual Studio 2019)
2. From `file-access/` run `node prepare.js` to create the files to be accessed
3. Electron:
   * Run `yarn` to install all dependencies
   * Run `npm start` to start the application
   * Press on the `Start` button   
   WPF WebView2:
   * Install nuget dependencies
   * Press F5
   * Press on the `Start` button
   WPF:
   * Press F5
   * Press on the `Start` button
   All tests generate a results file in the form of `TECHNOLOGY-FILESIZE-results.csv` under `file-access/`