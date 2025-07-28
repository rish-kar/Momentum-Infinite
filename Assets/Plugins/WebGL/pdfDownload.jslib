// Assets/Plugins/WebGL/pdfDownload.jslib

mergeInto(LibraryManager.library, {
  DownloadPdfFile: function(namePtr, dataPtr) {
    // namePtr and dataPtr are pointers into the WebGL heap, so we convert:
    var name = UTF8ToString(namePtr);
    var base64 = UTF8ToString(dataPtr);

    // Create a blob and trigger download
    var content = atob(base64);
    var buffer = new Uint8Array(content.length);
    for (var i = 0; i < content.length; ++i) {
      buffer[i] = content.charCodeAt(i);
    }
    var blob = new Blob([buffer], { type: "application/pdf" });
    var url = URL.createObjectURL(blob);

    // Create a temporary <a> to download
    var a = document.createElement("a");
    a.href = url;
    a.download = name;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
});
