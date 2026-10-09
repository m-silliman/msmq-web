import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const source = await readFile(new URL('../MsMqApp/wwwroot/js/message-list.js', import.meta.url), 'utf8');
const { attach, detach } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

test('scrolling pauses browsing, requests one page near the bottom, and cleans up', async () => {
    let scroll;
    const element = {
        dataset: { listVersion: '3', autoLoad: 'true' },
        addEventListener: (_, handler) => { scroll = handler; },
        removeEventListener: (_, handler) => { assert.equal(handler, scroll); scroll = null; }
    };
    const calls = [];
    let release;
    const dotnet = { invokeMethodAsync: async (...args) => {
        calls.push(args);
        if (args[2]) await new Promise(resolve => { release = resolve; });
    } };
    attach(element, dotnet);
    const grid = { classList: { contains: () => true }, scrollTop: 0, clientHeight: 400, scrollHeight: 4000 };
    await scroll({ target: grid });
    assert.equal(calls.length, 0);
    grid.scrollTop = 200;
    await scroll({ target: grid });
    await scroll({ target: grid });
    assert.deepEqual(calls, [['BrowseMessagesAsync', 3, false]]);
    grid.scrollTop = 3500;
    const pending = scroll({ target: grid });
    await scroll({ target: grid });
    assert.deepEqual(calls[1], ['BrowseMessagesAsync', 3, true]);
    assert.equal(calls.length, 2);
    release();
    await pending;
    element.dataset.autoLoad = 'false';
    await scroll({ target: grid });
    assert.equal(calls.length, 2);
    detach(element);
    assert.equal(scroll, null);
});
