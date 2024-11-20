mergeInto(LibraryManager.library, {
    NotifyLineSensor: function (dataSensor) {
        const data = UTF8ToString(dataSensor);
        const event = new CustomEvent("UnityData", { detail: data });
        window.dispatchEvent(event);
        return data;
    }
});