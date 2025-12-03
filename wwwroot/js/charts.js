// charts.js - Chart.js helpers for ResQLink Dashboard
// Immediately define the global object

window.resqCharts = window.resqCharts || {
    charts: {},

    // Create or update a line chart
    createLineChart: function (canvasId, labels, datasets, options) {
        console.log('Creating line chart:', canvasId);
        this.destroyChart(canvasId);
        
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas element not found:', canvasId);
            return null;
        }

        const defaultOptions = {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 15,
                        font: { size: 11, family: 'Poppins, sans-serif' }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    titleFont: { size: 13, family: 'Poppins, sans-serif' },
                    bodyFont: { size: 12, family: 'Poppins, sans-serif' },
                    cornerRadius: 8
                }
            },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: { font: { size: 10, family: 'Poppins, sans-serif' } }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: '#f3f4f6' },
                    ticks: { font: { size: 10, family: 'Poppins, sans-serif' } }
                }
            }
        };

        const mergedOptions = Object.assign({}, defaultOptions, options || {});

        try {
            this.charts[canvasId] = new Chart(ctx, {
                type: 'line',
                data: { labels: labels, datasets: datasets },
                options: mergedOptions
            });
            console.log('Chart created successfully:', canvasId);
            return this.charts[canvasId];
        } catch (error) {
            console.error('Error creating chart:', error);
            return null;
        }
    },

    // Create or update a bar chart
    createBarChart: function (canvasId, labels, datasets, options) {
        console.log('Creating bar chart:', canvasId);
        this.destroyChart(canvasId);
        
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas element not found:', canvasId);
            return null;
        }

        const defaultOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 15,
                        font: { size: 11, family: 'Poppins, sans-serif' }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    cornerRadius: 8
                }
            },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: { font: { size: 10, family: 'Poppins, sans-serif' } }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: '#f3f4f6' },
                    ticks: { font: { size: 10, family: 'Poppins, sans-serif' } }
                }
            }
        };

        const mergedOptions = Object.assign({}, defaultOptions, options || {});

        try {
            this.charts[canvasId] = new Chart(ctx, {
                type: 'bar',
                data: { labels: labels, datasets: datasets },
                options: mergedOptions
            });
            console.log('Chart created successfully:', canvasId);
            return this.charts[canvasId];
        } catch (error) {
            console.error('Error creating chart:', error);
            return null;
        }
    },

    // Create or update a doughnut chart
    createDoughnutChart: function (canvasId, labels, data, backgroundColors, options) {
        console.log('Creating doughnut chart:', canvasId);
        this.destroyChart(canvasId);
        
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas element not found:', canvasId);
            return null;
        }

        const defaultOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 12,
                        font: { size: 10, family: 'Poppins, sans-serif' }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    padding: 12,
                    cornerRadius: 8
                }
            }
        };

        const mergedOptions = Object.assign({}, defaultOptions, options || {});

        try {
            this.charts[canvasId] = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: labels,
                    datasets: [{
                        data: data,
                        backgroundColor: backgroundColors,
                        borderWidth: 2,
                        borderColor: '#fff'
                    }]
                },
                options: mergedOptions
            });
            console.log('Chart created successfully:', canvasId);
            return this.charts[canvasId];
        } catch (error) {
            console.error('Error creating chart:', error);
            return null;
        }
    },

    // Destroy a specific chart
    destroyChart: function (canvasId) {
        if (this.charts[canvasId]) {
            try {
                this.charts[canvasId].destroy();
                delete this.charts[canvasId];
                console.log('Chart destroyed:', canvasId);
            } catch (error) {
                console.error('Error destroying chart:', error);
            }
        }
    },

    // Destroy all charts
    destroyAllCharts: function () {
        console.log('Destroying all charts');
        Object.keys(this.charts).forEach(id => {
            try {
                this.charts[id].destroy();
            } catch (error) {
                console.error('Error destroying chart:', id, error);
            }
        });
        this.charts = {};
    },

    // Update chart data
    updateChart: function (canvasId, labels, datasets) {
        const chart = this.charts[canvasId];
        if (!chart) {
            console.warn('Chart not found for update:', canvasId);
            return false;
        }

        try {
            chart.data.labels = labels;
            chart.data.datasets = datasets;
            chart.update();
            console.log('Chart updated:', canvasId);
            return true;
        } catch (error) {
            console.error('Error updating chart:', error);
            return false;
        }
    }
};

// Log that the script has loaded
console.log('resqCharts module loaded successfully');
console.log('Chart.js version:', typeof Chart !== 'undefined' ? Chart.version : 'NOT LOADED');  