// Dosya indirme / yazdırma JS interop yardımcıları
// Not: window üzerine atanır ki Blazor enhanced navigation sonrası da erişilebilsin.
(function () {
    function downloadFile(filename, base64Content, contentType) {
        try {
            const byteNumbers = base64ToUint8Array(base64Content);
            const blob = new Blob([byteNumbers], { type: contentType || 'application/octet-stream' });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            setTimeout(function () { URL.revokeObjectURL(url); }, 10000);
        } catch (e) {
            console.error('downloadFile hatası:', e);
            throw e;
        }
    }

    function base64ToUint8Array(base64) {
        // Whitespace ve satır sonlarını temizle (Blazor JSON serializer bazen ekleyebiliyor)
        var clean = String(base64).replace(/[\r\n\s]/g, '');
        // Base64 padding düzelt
        var pad = clean.length % 4;
        if (pad === 2) clean += '==';
        else if (pad === 3) clean += '=';
        else if (pad === 1) throw new Error('Geçersiz base64 uzunluğu');

        var binary = atob(clean);
        var len = binary.length;
        var arr = new Uint8Array(len);
        for (var i = 0; i < len; i++) arr[i] = binary.charCodeAt(i);
        return arr;
    }

    function downloadFileFromBytes(filename, contentType, bytes) {
        try {
            let byteArray;
            if (bytes instanceof Uint8Array) {
                byteArray = bytes;
            } else if (Array.isArray(bytes)) {
                byteArray = new Uint8Array(bytes);
            } else if (bytes && Array.isArray(bytes.data)) {
                byteArray = new Uint8Array(bytes.data);
            } else if (typeof bytes === 'string') {
                byteArray = base64ToUint8Array(bytes);
            } else {
                throw new Error('Geçersiz byte dizisi (tip: ' + typeof bytes + ')');
            }

            const blob = new Blob([byteArray], { type: contentType || 'application/octet-stream' });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            setTimeout(function () { URL.revokeObjectURL(url); }, 10000);
        } catch (e) {
            console.error('downloadFileFromBytes hatası:', e);
            throw e;
        }
    }

    function downloadFileFromBase64(filename, base64Content) {
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
    }

    function downloadFileFromStream(filename, base64Content) {
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
    }

    function printContent(htmlContent) {
        const printWindow = window.open('', '_blank', 'width=900,height=700');
        printWindow.document.write(htmlContent);
        printWindow.document.close();
        printWindow.focus();
        setTimeout(function () { printWindow.print(); }, 500);
    }

    function copyToClipboard(text) {
        navigator.clipboard.writeText(text).then(function () {
            console.log('Clipboard copy successful');
        }, function (err) {
            console.error('Clipboard copy failed:', err);
        });
    }

    function goBackInHistory() {
        window.history.back();
    }

    function setLocalStorageItem(key, value) {
        window.localStorage.setItem(key, value);
    }

    function getLocalStorageItem(key) {
        return window.localStorage.getItem(key);
    }

    function printBase64Pdf(base64Content) {
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'application/pdf' });
        const blobUrl = URL.createObjectURL(blob);
        const printWindow = window.open(blobUrl, '_blank');
        if (printWindow) {
            printWindow.addEventListener('load', function () {
                setTimeout(function () { printWindow.print(); }, 500);
            });
        }
    }

    // Birden fazla dosyanın içeriğini tek bir yazdırma penceresinde göster
    // items: [{ name, base64, mime }]
    function printDocumentFiles(items) {
        if (!items || items.length === 0) return;

        const blobUrls = [];
        const sections = items.map(function (it) {
            const byteCharacters = atob(it.base64);
            const byteNumbers = new Array(byteCharacters.length);
            for (let i = 0; i < byteCharacters.length; i++) {
                byteNumbers[i] = byteCharacters.charCodeAt(i);
            }
            const byteArray = new Uint8Array(byteNumbers);
            const blob = new Blob([byteArray], { type: it.mime || 'application/octet-stream' });
            const url = URL.createObjectURL(blob);
            blobUrls.push(url);

            const mime = (it.mime || '').toLowerCase();
            let body = '';
            if (mime.startsWith('image/')) {
                body = '<img src="' + url + '" style="max-width:100%; height:auto; display:block; margin:0 auto;" />';
            } else if (mime === 'application/pdf') {
                body = '<embed src="' + url + '" type="application/pdf" style="width:100%; height:95vh;" />';
            } else {
                body = '<div style="padding:24px; border:1px dashed #999; text-align:center;">'
                    + '<p>Bu dosya türü tarayıcıda görüntülenemiyor.</p>'
                    + '<p><strong>' + (it.name || '') + '</strong></p>'
                    + '<p><a href="' + url + '" download="' + (it.name || 'belge') + '">Dosyayı indir</a></p>'
                    + '</div>';
            }

            return ''
                + '<section style="page-break-after:always; margin-bottom:24px;">'
                + '  <h3 style="font-family:Arial,sans-serif; border-bottom:1px solid #ccc; padding-bottom:6px;">' + (it.name || '') + '</h3>'
                + '  ' + body
                + '</section>';
        }).join('');

        const html = ''
            + '<!doctype html><html><head><meta charset="utf-8" />'
            + '<title>Belgeleri Yazdır</title>'
            + '<style>'
            + '  body { font-family: Arial, sans-serif; margin: 16px; }'
            + '  @media print { section { page-break-after: always; } }'
            + '</style>'
            + '</head><body>'
            + sections
            + '</body></html>';

        const printWindow = window.open('', '_blank');
        if (!printWindow) {
            alert('Yazdırma penceresi açılamadı. Lütfen pop-up engelini kapatın.');
            return;
        }
        printWindow.document.open();
        printWindow.document.write(html);
        printWindow.document.close();

        printWindow.addEventListener('load', function () {
            setTimeout(function () {
                try { printWindow.focus(); printWindow.print(); } catch (e) { console.error(e); }
            }, 800);
        });

        printWindow.addEventListener('beforeunload', function () {
            blobUrls.forEach(function (u) { URL.revokeObjectURL(u); });
        });
    }

    function scrollToElementById(elementId) {
        try {
            const el = document.getElementById(elementId);
            if (!el) return;
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            el.classList.add('odeme-vurgu-flash');
            setTimeout(function () {
                el.classList.remove('odeme-vurgu-flash');
            }, 2500);
        } catch (e) {
            console.warn('scrollToElementById:', e);
        }
    }

    // window scope'a açıkça bağla — Blazor IJSRuntime bu globalleri arar.
    window.downloadFile = downloadFile;
    window.downloadFileFromBytes = downloadFileFromBytes;
    window.downloadFileFromBase64 = downloadFileFromBase64;
    window.downloadFileFromStream = downloadFileFromStream;
    window.printContent = printContent;
    window.copyToClipboard = copyToClipboard;
    window.goBackInHistory = goBackInHistory;
    window.setLocalStorageItem = setLocalStorageItem;
    window.getLocalStorageItem = getLocalStorageItem;
    window.printBase64Pdf = printBase64Pdf;
    window.printDocumentFiles = printDocumentFiles;
    window.scrollToElementById = scrollToElementById;
})();


