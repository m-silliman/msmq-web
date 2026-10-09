// Sidebar resize functionality
let isResizing = false;
let dotNetHelper = null;
let minWidth = 200;
let maxWidth = 500;

window.startSidebarResize = function (helper) {
    isResizing = true;
    dotNetHelper = helper;
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
};

window.handleSidebarResize = function (e) {
    if (!isResizing || !dotNetHelper) return;

    const newWidth = Math.max(minWidth, Math.min(maxWidth, e.clientX));

    // Update sidebar width via Blazor
    dotNetHelper.invokeMethodAsync('UpdateSidebarWidthAsync', newWidth);
};

window.stopSidebarResize = function () {
    if (!isResizing) return;

    isResizing = false;
    document.body.style.cursor = '';
    document.body.style.userSelect = '';

    if (dotNetHelper) {
        dotNetHelper.invokeMethodAsync('StopResize');
    }
};

window.downloadFileFromStream = async function (fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: 'application/zip' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');

    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
};
