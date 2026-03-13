var SocketIOPlugin = {

    $SocketState: {
        socket: null,
        receiverObject: 'WebGLSocketReceiver'
    },

    /**
     * Connects to a Socket.IO server using the browser's socket.io-client library.
     * The library must be loaded via <script> tag in the WebGL template.
     * Auth token is passed in the Socket.IO CONNECT handshake.
     */
    SocketIO_Connect: function (urlPtr, authTokenPtr) {
        var url = UTF8ToString(urlPtr);
        var authToken = UTF8ToString(authTokenPtr);

        if (typeof io === 'undefined') {
            console.error('[SocketIOPlugin] socket.io-client not loaded. Include it in your WebGL template.');
            Module.SendMessage(SocketState.receiverObject, 'OnSocketErrorReceived',
                'Socket.IO client library not loaded');
            return;
        }

        if (SocketState.socket) {
            SocketState.socket.disconnect();
            SocketState.socket = null;
        }

        console.log('[SocketIOPlugin] Connecting to ' + url);

        var opts = {
            transports: ['websocket'],
            reconnection: false
        };

        if (authToken && authToken.length > 0) {
            opts.auth = { token: authToken };
        }

        SocketState.socket = io(url, opts);

        SocketState.socket.on('connect', function () {
            console.log('[SocketIOPlugin] Connected, id=' + SocketState.socket.id);
            Module.SendMessage(SocketState.receiverObject, 'OnSocketConnectedCallback', '');
        });

        SocketState.socket.on('disconnect', function (reason) {
            console.log('[SocketIOPlugin] Disconnected: ' + reason);
            Module.SendMessage(SocketState.receiverObject, 'OnSocketDisconnectedCallback', reason);
        });

        SocketState.socket.on('connect_error', function (err) {
            console.error('[SocketIOPlugin] Connection error: ' + err.message);
            Module.SendMessage(SocketState.receiverObject, 'OnSocketErrorReceived',
                err.message || 'Connection error');
        });

        SocketState.socket.onAny(function (eventName, data) {
            var payload;
            if (typeof data === 'object' && data !== null) {
                payload = JSON.stringify(data);
            } else if (typeof data === 'string') {
                payload = data;
            } else {
                payload = '{}';
            }

            var msg = JSON.stringify({ e: eventName, d: payload });
            Module.SendMessage(SocketState.receiverObject, 'OnSocketMessage', msg);
        });
    },

    /**
     * Emits a named event with a JSON payload string to the server.
     */
    SocketIO_Send: function (eventNamePtr, payloadPtr) {
        var eventName = UTF8ToString(eventNamePtr);
        var payload = UTF8ToString(payloadPtr);

        if (!SocketState.socket || !SocketState.socket.connected) {
            console.warn('[SocketIOPlugin] Cannot send, not connected');
            return;
        }

        var parsed;
        try {
            parsed = JSON.parse(payload);
        } catch (e) {
            parsed = payload;
        }

        SocketState.socket.emit(eventName, parsed);
    },

    /**
     * Disconnects the Socket.IO connection.
     */
    SocketIO_Disconnect: function () {
        if (SocketState.socket) {
            SocketState.socket.disconnect();
            SocketState.socket = null;
        }
    },

    /**
     * Returns the server URL, checking ?server= query param first,
     * then falling back to window.location.origin.
     */
    WebGL_GetServerUrl: function () {
        var url = '';
        try {
            var params = new URLSearchParams(window.location.search);
            url = params.get('server') || window.location.origin;
        } catch (e) {
            url = window.location.origin;
        }
        var bufferSize = lengthBytesUTF8(url) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(url, buffer, bufferSize);
        return buffer;
    }
};

autoAddDeps(SocketIOPlugin, '$SocketState');
mergeInto(LibraryManager.library, SocketIOPlugin);
