// Filo KPI Dashboard chart helpers
window.filoKpiCharts = (function () {
    const instances = {};

    function destroy(id) {
        if (instances[id]) {
            try { instances[id].destroy(); } catch (e) { }
            delete instances[id];
        }
    }

    function destroyAll() {
        Object.keys(instances).forEach(destroy);
    }

    function fmtTL(v) {
        return new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(v) + ' ₺';
    }

    function renderHakedisTip(canvasId, labels, data) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        instances[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: ['#198754', '#dc3545', '#0dcaf0'],
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom' },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return ctx.label + ': ' + fmtTL(ctx.parsed);
                            }
                        }
                    }
                }
            }
        });
    }

    function renderSeferTrend(canvasId, labels, data) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Günlük Sefer',
                    data: data,
                    backgroundColor: 'rgba(13, 110, 253, 0.6)',
                    borderColor: '#0d6efd',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: { beginAtZero: true, ticks: { precision: 0 } }
                }
            }
        });
    }

    function renderKurumTrend(canvasId, labels, datasets) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        const palette = [
            '#0d6efd', '#198754', '#dc3545', '#fd7e14', '#6f42c1',
            '#20c997', '#ffc107', '#0dcaf0', '#d63384', '#6c757d',
            '#212529', '#adb5bd'
        ];
        const ds = (datasets || []).map((d, i) => ({
            label: d.label,
            data: d.data,
            borderColor: palette[i % palette.length],
            backgroundColor: palette[i % palette.length] + '33',
            borderWidth: 2,
            tension: 0.25,
            fill: false,
            pointRadius: 3
        }));
        instances[canvasId] = new Chart(ctx, {
            type: 'line',
            data: { labels: labels, datasets: ds },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { position: 'bottom' },
                    tooltip: {
                        callbacks: {
                            label: function (c) { return c.dataset.label + ': ' + fmtTL(c.parsed.y); }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { callback: function (v) { return fmtTL(v); } }
                    }
                }
            }
        });
    }

    function renderKurumToplamBar(canvasId, labels, data) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Yıllık Toplam',
                    data: data,
                    backgroundColor: 'rgba(25, 135, 84, 0.6)',
                    borderColor: '#198754',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: 'y',
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (c) { return fmtTL(c.parsed.x); }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: { callback: function (v) { return fmtTL(v); } }
                    }
                }
            }
        });
    }

    function renderVadeKategori(canvasId, labels, data) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        const palette = [
            '#dc3545', '#fd7e14', '#ffc107', '#198754', '#0dcaf0',
            '#0d6efd', '#6f42c1', '#d63384', '#20c997', '#6c757d'
        ];
        instances[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: labels.map((_, i) => palette[i % palette.length]),
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'right' },
                    tooltip: {
                        callbacks: {
                            label: function (c) {
                                const total = c.dataset.data.reduce((a, b) => a + b, 0);
                                const pct = total > 0 ? ((c.parsed / total) * 100).toFixed(1) : 0;
                                return c.label + ': ' + c.parsed + ' (%' + pct + ')';
                            }
                        }
                    }
                }
            }
        });
    }

    function renderVadePencere(canvasId, labels, data) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        // Renk skalası: kırmızıdan yeşile
        const colors = [
            '#dc3545', '#fd7e14', '#ffc107', '#0dcaf0',
            '#198754', '#20c997', '#6c757d'
        ];
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Belge Sayısı',
                    data: data,
                    backgroundColor: labels.map((_, i) => colors[i % colors.length]),
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (c) { return c.parsed.y + ' belge'; }
                        }
                    }
                },
                scales: {
                    y: { beginAtZero: true, ticks: { precision: 0 } }
                }
            }
        });
    }

    function renderSoforSkor(canvasId, labels, data) {
        destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        // Skor renk skalası: 0-40 kırmızı, 40-60 turuncu, 60-80 mavi, 80-100 yeşil
        const colors = data.map(v => {
            if (v >= 80) return '#198754';
            if (v >= 60) return '#0dcaf0';
            if (v >= 40) return '#fd7e14';
            return '#dc3545';
        });
        instances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Performans Skoru',
                    data: data,
                    backgroundColor: colors,
                    borderWidth: 1
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (c) { return 'Skor: ' + c.parsed.x.toFixed(1); }
                        }
                    }
                },
                scales: {
                    x: { beginAtZero: true, max: 100, ticks: { precision: 0 } }
                }
            }
        });
    }

    return {
        renderHakedisTip: renderHakedisTip,
        renderSeferTrend: renderSeferTrend,
        renderKurumTrend: renderKurumTrend,
        renderKurumToplamBar: renderKurumToplamBar,
        renderVadeKategori: renderVadeKategori,
        renderVadePencere: renderVadePencere,
        renderSoforSkor: renderSoforSkor,
        destroyAll: destroyAll
    };
})();
