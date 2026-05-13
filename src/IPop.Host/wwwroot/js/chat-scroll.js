window.ipopChatScrollToBottom = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    requestAnimationFrame(() => {
        el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
    });
};
