// Karlılık Raporu — Chart.js render helpers
window.karlilikCharts = (function () {
    const instances = {};

    function destroy(id) {
        if (instances[id]) {
            try { instances[id].destroy(); } catch { }
            delete instances[id];
        }
    }

    function renderSahiplikDonut(canvasId, labels, data) {
        destroy(canvasId);
        const el = document.getElementById(canvasId);
        if (!el) return;
        instances[canvasId] = new Chart(el, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: ['#0d6efd', '#6f42c1', '#fd7e14', '#20c997'],
                    borderWidth: 1
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
                                const v = ctx.parsed || 0;
                                const total = ctx.dataset.data.reduce((a, b) => a + b, 0);
                                const pct = total > 0 ? ((v / total) * 100).toFixed(1) : 0;
                                return `${ctx.label}: ${v.toLocaleString('tr-TR')} ₺ (%${pct})`;
                            }
                        }
                    }
                }
            }
        });
    }

    function renderAylikTrend(canvasId, labels, gelir, gider, karZarar) {
        destroy(canvasId);
        const el = document.getElementById(canvasId);
        if (!el) return;
        instances[canvasId] = new Chart(el, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    { type: 'bar', label: 'Gelir', data: gelir, backgroundColor: 'rgba(25, 135, 84, 0.7)', borderColor: '#198754', borderWidth: 1, order: 2 },
                    { type: 'bar', label: 'Toplam Gider', data: gider, backgroundColor: 'rgba(220, 53, 69, 0.7)', borderColor: '#dc3545', borderWidth: 1, order: 2 },
                    { type: 'line', label: 'Kar/Zarar', data: karZarar, borderColor: '#0d6efd', backgroundColor: 'rgba(13, 110, 253, 0.1)', tension: 0.3, fill: false, borderWidth: 2, pointRadius: 4, order: 1 }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { position: 'bottom' },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                const v = ctx.parsed.y || 0;
                                return `${ctx.dataset.label}: ${v.toLocaleString('tr-TR')} ₺`;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (v) { return v.toLocaleString('tr-TR'); }
                        }
                    }
                }
            }
        });
    }

    function renderKurumBar(canvasId, labels, data) {
        destroy(canvasId);
        const el = document.getElementById(canvasId);
        if (!el) return;
        instances[canvasId] = new Chart(el, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{ label: 'Gelir', data: data, backgroundColor: 'rgba(13, 110, 253, 0.7)', borderColor: '#0d6efd', borderWidth: 1 }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: 'y',
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (ctx) {
                                return `${ctx.parsed.x.toLocaleString('tr-TR')} ₺`;
                            }
                        }
                    }
                },
                scales: {
                    x: { ticks: { callback: v => v.toLocaleString('tr-TR') } }
                }
            }
        });
    }

    return {
        renderSahiplikDonut: renderSahiplikDonut,
        renderAylikTrend: renderAylikTrend,
        renderKurumBar: renderKurumBar,
        destroy: destroy
    };
})();
