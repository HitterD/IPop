window.ipopTheme = {
    get: function () {
        var theme = localStorage.ipopTheme || document.documentElement.dataset.theme || 'light';
        document.documentElement.dataset.theme = theme;
        document.body.dataset.theme = theme;
        return theme;
    },
    set: function (theme) {
        var next = theme === 'dark' ? 'dark' : 'light';
        localStorage.ipopTheme = next;
        document.documentElement.dataset.theme = next;
        document.body.dataset.theme = next;
    },
    scrollToComposer: function (id) {
        var element = document.getElementById(id);
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'end' });
        }
    }
};
