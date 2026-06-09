window.renderGelirGiderChart = (labels, gelir, gider) => {
    const ctx = document.getElementById('gelirGiderChart');
    if (!ctx) return;

    if (window._gelirGiderChart) {
        window._gelirGiderChart.destroy();
    }

    window._gelirGiderChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Gelir',
                    data: gelir,
                    borderColor: 'rgb(25,135,84)',
                    backgroundColor: 'rgba(25,135,84,0.1)',
                    tension: 0.2
                },
                {
                    label: 'Gider',
                    data: gider,
                    borderColor: 'rgb(220,53,69)',
                    backgroundColor: 'rgba(220,53,69,0.1)',
                    tension: 0.2
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'top' } }
        }
    });
};
