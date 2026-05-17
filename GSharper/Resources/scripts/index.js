window.assistant = {
    diffEditor: null,
    originalModel: null,
    modifiedModel: null,
    setModel: function (original, modified, language, readOnly) {
        assistant.originalModel = monaco.editor.createModel(original, language);
        assistant.modifiedModel = monaco.editor.createModel(modified, language);

        assistant.diffEditor.setModel({
            original: assistant.originalModel,
            modified: assistant.modifiedModel
        });
        assistant.diffEditor.updateOptions({ readOnly: !!readOnly });
    },
    getModifiedText: function () {
        return assistant.modifiedModel.getValue();
    }
};

require(['vs/editor/editor.main'], function () {
    var diffEditor = monaco.editor.createDiffEditor(document.getElementById('container'), {
        enableSplitViewResizing: true,
        ignoreTrimWhitespace: false,
        renderSideBySide: true,
        automaticLayout: true,
        theme: 'vs-dark',
    });

    diffEditor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, function () {
        window.chrome.webview.postMessage({
            action: 'save-content'
        });
    });

    assistant.diffEditor = diffEditor;
    window.chrome.webview.postMessage({
        action: 'init-monaco-editor'
    });
});