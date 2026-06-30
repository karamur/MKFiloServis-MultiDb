// SignalR Araç Takip Hub Client - Gerçek Zamanlı Konum Güncellemeleri

window.signalRHelper = {
    connection: null,
    dotNetRef: null,

    // Hub bağlantısını başlat
    startConnection: async function (dotNetRef) {
        this.dotNetRef = dotNetRef;

        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            console.log("SignalR zaten bağlı");
            return true;
        }

        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/aractakip")
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Otomatik yeniden bağlanma
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Event handler'ları kaydet
            this.registerEventHandlers();

            // Bağlan
            await this.connection.start();
            console.log("SignalR bağlantısı başarılı");
            
            // Blazor'a bildir
            if (this.dotNetRef) {
                await this.dotNetRef.invokeMethodAsync('OnSignalRConnected');
            }
            
            return true;
        } catch (err) {
            console.error("SignalR bağlantı hatası:", err);
            return false;
        }
    },

    // Event handler'ları kaydet
    registerEventHandlers: function () {
        const connection = this.connection;
        const dotNetRef = this.dotNetRef;

        // Bağlantı kapandığında
        connection.onclose(async (error) => {
            console.log("SignalR bağlantısı kapandı:", error);
            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync('OnSignalRDisconnected', error?.message || null);
            }
        });

        // Yeniden bağlanma denerken
        connection.onreconnecting((error) => {
            console.log("SignalR yeniden bağlanıyor:", error);
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnSignalRReconnecting', error?.message || null);
            }
        });

        // Yeniden bağlandığında
        connection.onreconnected((connectionId) => {
            console.log("SignalR yeniden bağlandı:", connectionId);
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnSignalRReconnected');
            }
        });

        // === Server'dan gelen mesajlar ===

        // Tek araç konum güncellemesi
        connection.on("KonumGuncellendi", async (guncelleme) => {
            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync('OnKonumGuncellendi', guncelleme);
            }
        });

        // Belirli araç konum güncellemesi
        connection.on("AracKonumGuncellendi", async (guncelleme) => {
            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync('OnAracKonumGuncellendi', guncelleme);
            }
        });

        // Toplu konum güncellemesi
        connection.on("TopluKonumGuncellendi", async (guncellemeler) => {
            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync('OnTopluKonumGuncellendi', guncellemeler);
            }
        });

        // Alarm oluştu
        connection.on("AlarmOlustu", async (alarm) => {
            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync('OnAlarmOlustu', alarm);
            }
        });

        // Belirli araç alarmı
        connection.on("AracAlarmOlustu", async (alarm) => {
            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync('OnAracAlarmOlustu', alarm);
            }
        });

        // Bölge olayı
        connection.on("BolgeOlayiOlustu", async (olay) => {
            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync('OnBolgeOlayiOlustu', olay);
            }
        });

        // Sistem mesajı
        connection.on("SistemMesaji", async (mesaj) => {
            if (dotNetRef) {
                await dotNetRef.invokeMethodAsync('OnSistemMesaji', mesaj);
            }
        });
    },

    // Bağlantıyı kapat
    stopConnection: async function () {
        if (this.connection) {
            try {
                await this.connection.stop();
                console.log("SignalR bağlantısı kapatıldı");
            } catch (err) {
                console.error("SignalR kapatma hatası:", err);
            }
        }
    },

    // Belirli bir aracı takip et
    takipBaslat: async function (aracId) {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                await this.connection.invoke("TakipBaslat", aracId);
                console.log("Araç takibi başlatıldı:", aracId);
                return true;
            } catch (err) {
                console.error("Takip başlatma hatası:", err);
                return false;
            }
        }
        return false;
    },

    // Takibi durdur
    takipDurdur: async function (aracId) {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                await this.connection.invoke("TakipDurdur", aracId);
                console.log("Araç takibi durduruldu:", aracId);
                return true;
            } catch (err) {
                console.error("Takip durdurma hatası:", err);
                return false;
            }
        }
        return false;
    },

    // Çoklu araç takip et
    cokluTakipBaslat: async function (aracIdler) {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                await this.connection.invoke("CokluTakipBaslat", aracIdler);
                console.log("Çoklu araç takibi başlatıldı:", aracIdler.length);
                return true;
            } catch (err) {
                console.error("Çoklu takip başlatma hatası:", err);
                return false;
            }
        }
        return false;
    },

    // Tüm takipleri durdur
    tumTakipleriDurdur: async function () {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                await this.connection.invoke("TumTakipleriDurdur");
                console.log("Tüm takipler durduruldu");
                return true;
            } catch (err) {
                console.error("Takip durdurma hatası:", err);
                return false;
            }
        }
        return false;
    },

    // Bölge takip et
    bolgeTakipBaslat: async function (bolgeId) {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                await this.connection.invoke("BolgeTakipBaslat", bolgeId);
                console.log("Bölge takibi başlatıldı:", bolgeId);
                return true;
            } catch (err) {
                console.error("Bölge takip başlatma hatası:", err);
                return false;
            }
        }
        return false;
    },

    // Bölge takibini durdur
    bolgeTakipDurdur: async function (bolgeId) {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                await this.connection.invoke("BolgeTakipDurdur", bolgeId);
                console.log("Bölge takibi durduruldu:", bolgeId);
                return true;
            } catch (err) {
                console.error("Bölge takip durdurma hatası:", err);
                return false;
            }
        }
        return false;
    },

    // Bağlantı durumunu kontrol et
    isConnected: function () {
        return this.connection && this.connection.state === signalR.HubConnectionState.Connected;
    },

    // Bağlantı durumunu al
    getConnectionState: function () {
        if (!this.connection) return "Disconnected";
        switch (this.connection.state) {
            case signalR.HubConnectionState.Connected: return "Connected";
            case signalR.HubConnectionState.Connecting: return "Connecting";
            case signalR.HubConnectionState.Disconnected: return "Disconnected";
            case signalR.HubConnectionState.Disconnecting: return "Disconnecting";
            case signalR.HubConnectionState.Reconnecting: return "Reconnecting";
            default: return "Unknown";
        }
    }
};
