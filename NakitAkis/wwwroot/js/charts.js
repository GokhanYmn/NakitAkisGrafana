window.createBarChart = (canvasId, data) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing chart
    if (window.charts && window.charts[canvasId]) {
        window.charts[canvasId].destroy();
    }

    if (!window.charts) window.charts = {};

    window.charts[canvasId] = new Chart(ctx, {
        type: 'bar',
        data: data,
        options: {
            responsive: true,
            plugins: {
                title: {
                    display: true,
                    text: 'Nakit Akış Analizi - Bar Chart',
                    font: {
                        size: 16
                    }
                },
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) {
                            return '₺' + value.toLocaleString('tr-TR');
                        }
                    }
                }
            },
            animation: {
                duration: 1000,
                easing: 'easeOutQuart'
            }
        }
    });
};

window.createPieChart = (canvasId, data) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    if (window.charts && window.charts[canvasId]) {
        window.charts[canvasId].destroy();
    }

    if (!window.charts) window.charts = {};

    window.charts[canvasId] = new Chart(ctx, {
        type: 'pie',
        data: data,
        options: {
            responsive: true,
            plugins: {
                title: {
                    display: true,
                    text: 'Nakit Akış Dağılımı - Pie Chart',
                    font: {
                        size: 16
                    }
                },
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 20
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return context.label + ': ₺' + context.parsed.toLocaleString('tr-TR');
                        }
                    }
                }
            },
            animation: {
                duration: 1000,
                easing: 'easeOutQuart'
            }
        }
    });
};

window.createDoughnutChart = (canvasId, data) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    if (window.charts && window.charts[canvasId]) {
        window.charts[canvasId].destroy();
    }

    if (!window.charts) window.charts = {};

    window.charts[canvasId] = new Chart(ctx, {
        type: 'doughnut',
        data: data,
        options: {
            responsive: true,
            plugins: {
                title: {
                    display: true,
                    text: 'Nakit Akış Analizi - Doughnut Chart',
                    font: {
                        size: 16
                    }
                },
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 20
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return context.label + ': ₺' + context.parsed.toLocaleString('tr-TR');
                        }
                    }
                }
            },
            animation: {
                duration: 1000,
                easing: 'easeOutQuart'
            }
        }
    });
};