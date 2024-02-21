mergeInto(LibraryManager.library, {

    FlushIndexedDB: function () {
        _JS_FileSystem_Sync();
    },
});
