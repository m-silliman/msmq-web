// HomePage panel resize functionality
window.homePageResizing = false;
window.homePageDotNetRef = null;

window.setHomePageDotNetRef = function (dotNetRef) {
    window.homePageDotNetRef = dotNetRef;
};

window.startHomePageResize = function (dotNetRef) {
    window.homePageDotNetRef = dotNetRef;
    window.homePageResizing = true;
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
};

window.homePageResizeHandler = function (e) {
    if (!window.homePageResizing || !window.homePageDotNetRef) return;

    const container = document.querySelector('.home-content');
    if (!container) return;

    const containerWidth = container.offsetWidth;
    const mouseX = e.clientX;
    const containerLeft = container.getBoundingClientRect().left;
    const relativeX = mouseX - containerLeft;

    const widthPercent = Math.round((relativeX / containerWidth) * 100);
    const clampedPercent = Math.max(20, Math.min(60, widthPercent));

    window.homePageDotNetRef.invokeMethodAsync('UpdatePanelWidth', clampedPercent);
};

window.stopHomePageResize = function () {
    window.homePageResizing = false;
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
};
