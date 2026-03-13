var TelegramPlugin = {

    /**
     * Returns Telegram WebApp initData string for server-side validation.
     * The Telegram Web App SDK must be loaded via <script> in the WebGL template.
     */
    TMA_GetInitData: function () {
        var initData = '';
        if (typeof Telegram !== 'undefined' && Telegram.WebApp) {
            initData = Telegram.WebApp.initData || '';
        }
        var bufferSize = lengthBytesUTF8(initData) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(initData, buffer, bufferSize);
        return buffer;
    },

    /**
     * Returns the authenticated user's display name from Telegram WebApp.
     */
    TMA_GetUserDisplayName: function () {
        var name = 'Player';
        if (typeof Telegram !== 'undefined' && Telegram.WebApp &&
            Telegram.WebApp.initDataUnsafe && Telegram.WebApp.initDataUnsafe.user) {
            name = Telegram.WebApp.initDataUnsafe.user.first_name || 'Player';
        }
        var bufferSize = lengthBytesUTF8(name) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(name, buffer, bufferSize);
        return buffer;
    },

    /**
     * Returns the user's avatar URL (photo_url) if available.
     */
    TMA_GetUserAvatarUrl: function () {
        var url = '';
        if (typeof Telegram !== 'undefined' && Telegram.WebApp &&
            Telegram.WebApp.initDataUnsafe && Telegram.WebApp.initDataUnsafe.user) {
            url = Telegram.WebApp.initDataUnsafe.user.photo_url || '';
        }
        var bufferSize = lengthBytesUTF8(url) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(url, buffer, bufferSize);
        return buffer;
    },

    /**
     * Triggers Telegram haptic feedback.
     * @param stylePtr - "light", "medium", or "heavy"
     */
    TMA_HapticImpact: function (stylePtr) {
        if (typeof Telegram !== 'undefined' && Telegram.WebApp &&
            Telegram.WebApp.HapticFeedback) {
            var style = UTF8ToString(stylePtr);
            Telegram.WebApp.HapticFeedback.impactOccurred(style);
        }
    },

    /**
     * Opens the Telegram share dialog with a URL and text.
     */
    TMA_ShareUrl: function (urlPtr, textPtr) {
        if (typeof Telegram !== 'undefined' && Telegram.WebApp) {
            var url = UTF8ToString(urlPtr);
            var text = UTF8ToString(textPtr);
            var shareLink = 'https://t.me/share/url?url=' +
                encodeURIComponent(url) + '&text=' + encodeURIComponent(text);
            Telegram.WebApp.openTelegramLink(shareLink);
        }
    },

    /**
     * Signals Telegram that the Mini App is ready (removes loading indicator).
     */
    TMA_Ready: function () {
        if (typeof Telegram !== 'undefined' && Telegram.WebApp) {
            Telegram.WebApp.ready();
            Telegram.WebApp.expand();
        }
    },

    /**
     * Returns the current Telegram theme color scheme: "light" or "dark".
     */
    TMA_GetColorScheme: function () {
        var scheme = 'dark';
        if (typeof Telegram !== 'undefined' && Telegram.WebApp) {
            scheme = Telegram.WebApp.colorScheme || 'dark';
        }
        var bufferSize = lengthBytesUTF8(scheme) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(scheme, buffer, bufferSize);
        return buffer;
    }
};

mergeInto(LibraryManager.library, TelegramPlugin);
