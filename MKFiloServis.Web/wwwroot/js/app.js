// KOA Filo Servis - Genel JS yardımcı fonksiyonları

// Favoriler / Hızlı Erişim
window.favorites = {
    _key: 'crm-favorites',
    getAll: function () {
        try { return JSON.parse(localStorage.getItem(this._key) || '[]'); } catch { return []; }
    },
    add: function (url, title, icon) {
        var list = this.getAll();
        if (!list.some(function (f) { return f.url === url; })) {
            list.unshift({ url: url, title: title, icon: icon || 'bi-star' });
            if (list.length > 20) list = list.slice(0, 20);
            localStorage.setItem(this._key, JSON.stringify(list));
        }
        return list;
    },
    remove: function (url) {
        var list = this.getAll().filter(function (f) { return f.url !== url; });
        localStorage.setItem(this._key, JSON.stringify(list));
        return list;
    },
    isFavorite: function (url) {
        return this.getAll().some(function (f) { return f.url === url; });
    }
};

// Dark Mode
window.darkMode = {
    get: function () {
        return localStorage.getItem('crm-theme') || 'light';
    },
    set: function (theme) {
        localStorage.setItem('crm-theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
    },
    init: function () {
        const saved = localStorage.getItem('crm-theme') || 'light';
        document.documentElement.setAttribute('data-theme', saved);
        return saved;
    }
};

/**
 * Base64 kodlu içeriği tarayıcıda dosya olarak indirir.
 * @param {string} base64 - Base64 kodlu dosya içeriği
 * @param {string} fileName - İndirilecek dosya adı
 * @param {string} mimeType - MIME türü (örn: application/pdf)
 */
// Keyboard Shortcuts
window.keyboardShortcuts = {
    _handlers: {},
    _dotnet: null,
    init: function (dotnetRef) {
        this._dotnet = dotnetRef;
        var self = this;
        document.addEventListener('keydown', function (e) {
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable) {
                if (e.key === 'Escape') {
                    self._dotnet && self._dotnet.invokeMethodAsync('OnShortcut', 'Escape');
                }
                return;
            }
            var key = '';
            if (e.ctrlKey || e.metaKey) key += 'Ctrl+';
            if (e.altKey) key += 'Alt+';
            if (e.shiftKey) key += 'Shift+';
            key += (e.key.length === 1 ? e.key.toUpperCase() : e.key);
            if (key === 'Ctrl+/' || key === 'F1') {
                e.preventDefault();
                self._dotnet && self._dotnet.invokeMethodAsync('OnShortcut', 'Help');
            } else if (key === 'Escape') {
                self._dotnet && self._dotnet.invokeMethodAsync('OnShortcut', 'Escape');
            } else if (key === 'Ctrl+K') {
                e.preventDefault();
                self._dotnet && self._dotnet.invokeMethodAsync('OnShortcut', 'Search');
            } else if (key === 'Ctrl+D') {
                e.preventDefault();
                self._dotnet && self._dotnet.invokeMethodAsync('OnShortcut', 'Dashboard');
            } else if (key === 'Ctrl+N') {
                e.preventDefault();
                self._dotnet && self._dotnet.invokeMethodAsync('OnShortcut', 'New');
            } else if (key === 'Ctrl+B') {
                e.preventDefault();
                self._dotnet && self._dotnet.invokeMethodAsync('OnShortcut', 'ToggleSidebar');
            }
        });
    },
    dispose: function () { this._dotnet = null; }
};

window.downloadBase64File = function (base64, fileName, mimeType) {
    const byteChars = atob(base64);
    const byteArrays = [];
    for (let i = 0; i < byteChars.length; i += 512) {
        const slice = byteChars.slice(i, i + 512);
        const bytes = new Uint8Array(slice.length);
        for (let j = 0; j < slice.length; j++) {
            bytes[j] = slice.charCodeAt(j);
        }
        byteArrays.push(bytes);
    }
    const blob = new Blob(byteArrays, { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// Dashboard Widget Ayarları
window.dashboardWidgets = {
    _key: 'dashboard-widgets',
    _orderKey: 'dashboard-widgets-order',
    _defaults: {
        'hizli-islemler': true,
        'ozet-kartlar': true,
        'finansal-ozet': true,
        'grafikler': true,
        'vade-faturalar': true,
        'son-islemler': true
    },
    _defaultOrder: ['hizli-islemler', 'ozet-kartlar', 'finansal-ozet', 'grafikler', 'vade-faturalar', 'son-islemler'],
    getAll: function () {
        try {
            var saved = JSON.parse(localStorage.getItem(this._key) || '{}');
            return Object.assign({}, this._defaults, saved);
        } catch { return Object.assign({}, this._defaults); }
    },
    set: function (key, value) {
        var all = this.getAll();
        all[key] = value;
        localStorage.setItem(this._key, JSON.stringify(all));
    },
    isVisible: function (key) {
        return this.getAll()[key] !== false;
    },
    getOrder: function () {
        try {
            var saved = JSON.parse(localStorage.getItem(this._orderKey) || 'null');
            if (Array.isArray(saved) && saved.length === this._defaultOrder.length) return saved;
        } catch { }
        return null;
    },
    setOrder: function (order) {
        localStorage.setItem(this._orderKey, JSON.stringify(order));
    },
    reset: function () {
        localStorage.removeItem(this._key);
        localStorage.removeItem(this._orderKey);
    }
};

// Entegrasyon rehberi — canlı endpoint test fonksiyonu
window.analitikTest = async function (url, token) {
    try {
        var headers = { 'Content-Type': 'application/json' };
        if (token && token.trim()) headers['Authorization'] = 'Bearer ' + token.trim();
        var res = await fetch(url, { method: 'GET', headers: headers });
        var text = await res.text();
        try {
            var obj = JSON.parse(text);
            return JSON.stringify(obj, null, 2);
        } catch {
            return text;
        }
    } catch (e) {
        throw new Error(e.message || 'Bağlantı hatası');
    }
};
