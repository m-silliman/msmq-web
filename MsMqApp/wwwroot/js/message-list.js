const listeners = new WeakMap();

export function attach(element, dotnet) {
    detach(element);
    let pending = false;
    let pausedVersion;
    const onScroll = async event => {
        const grid = event.target;
        if (!grid.classList?.contains("message-list-grid") || grid.scrollTop < 64 || pending) return;
        const version = Number(element.dataset.listVersion);
        const loadMore = element.dataset.autoLoad === "true"
            && grid.scrollTop + grid.clientHeight >= grid.scrollHeight - 160;
        if (!loadMore && pausedVersion === version) return;
        pending = true;
        try {
            await dotnet.invokeMethodAsync("BrowseMessagesAsync", version, loadMore);
            pausedVersion = version;
        } catch {
            // The circuit or component may have gone away during a scroll.
        } finally {
            pending = false;
        }
    };
    element.addEventListener("scroll", onScroll, true);
    listeners.set(element, onScroll);
}

export function detach(element) {
    const listener = listeners.get(element);
    if (listener) element.removeEventListener("scroll", listener, true);
    listeners.delete(element);
}
