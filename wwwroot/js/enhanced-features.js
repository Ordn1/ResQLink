// ResQLink Enhanced Features JavaScript Helpers

/**
 * Download file from base64 encoded data
 * Used by Export Service
 */
window.downloadFileFromBase64 = function (fileName, base64Content, contentType) {
    try {
        const linkSource = `data:${contentType};base64,${base64Content}`;
        const downloadLink = document.createElement("a");
        downloadLink.href = linkSource;
        downloadLink.download = fileName;
        document.body.appendChild(downloadLink);
        downloadLink.click();
        document.body.removeChild(downloadLink);
        console.log(`File downloaded: ${fileName}`);
    } catch (error) {
        console.error('Download failed:', error);
        alert('Failed to download file. Please try again.');
    }
};

/**
 * Download file from byte array
 * Alternative download method
 */
window.downloadFile = function (fileName, byteArray) {
    try {
        const blob = new Blob([new Uint8Array(byteArray)], { type: 'application/octet-stream' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
        console.log(`File downloaded: ${fileName}`);
    } catch (error) {
        console.error('Download failed:', error);
        alert('Failed to download file. Please try again.');
    }
};

/**
 * Show toast notification
 * Used by Notification Service
 */
window.showToast = function (title, message, type = 'info', duration = 5000) {
    try {
        // Remove existing toasts
        const existingToasts = document.querySelectorAll('.toast-notification');
        existingToasts.forEach(toast => toast.remove());

        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <div class="toast-icon">
                    <i class="bi bi-${getToastIcon(type)}"></i>
                </div>
                <div class="toast-body">
                    <strong>${title}</strong>
                    <p>${message}</p>
                </div>
                <button class="toast-close" onclick="this.parentElement.remove()">
                    <i class="bi bi-x"></i>
                </button>
            </div>
        `;

        // Add to body
        document.body.appendChild(toast);

        // Trigger animation
        setTimeout(() => toast.classList.add('show'), 10);

        // Auto-remove after duration
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, duration);
    } catch (error) {
        console.error('Toast notification failed:', error);
    }
};

/**
 * Get appropriate icon for toast type
 */
function getToastIcon(type) {
    const icons = {
        'info': 'info-circle-fill',
        'success': 'check-circle-fill',
        'warning': 'exclamation-triangle-fill',
        'error': 'x-circle-fill',
        'critical': 'lightning-fill'
    };
    return icons[type] || 'info-circle-fill';
}

/**
 * Copy text to clipboard
 */
window.copyToClipboard = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        showToast('Copied', 'Text copied to clipboard', 'success', 2000);
        return true;
    } catch (error) {
        console.error('Copy failed:', error);
        showToast('Error', 'Failed to copy text', 'error', 3000);
        return false;
    }
};

/**
 * Print element by ID
 */
window.printElement = function (elementId) {
    try {
        const element = document.getElementById(elementId);
        if (!element) {
            console.error(`Element not found: ${elementId}`);
            return false;
        }

        const printWindow = window.open('', '_blank');
        printWindow.document.write(`
            <!DOCTYPE html>
            <html>
            <head>
                <title>Print</title>
                <style>
                    body { font-family: Arial, sans-serif; padding: 20px; }
                    @media print {
                        body { margin: 0; }
                    }
                </style>
            </head>
            <body>
                ${element.innerHTML}
            </body>
            </html>
        `);
        printWindow.document.close();
        printWindow.print();
        return true;
    } catch (error) {
        console.error('Print failed:', error);
        return false;
    }
};

/**
 * Scroll to element smoothly
 */
window.scrollToElement = function (elementId, offset = 0) {
    try {
        const element = document.getElementById(elementId);
        if (element) {
            const y = element.getBoundingClientRect().top + window.pageYOffset + offset;
            window.scrollTo({ top: y, behavior: 'smooth' });
            return true;
        }
        return false;
    } catch (error) {
        console.error('Scroll failed:', error);
        return false;
    }
};

/**
 * Get element dimensions
 */
window.getElementDimensions = function (elementId) {
    try {
        const element = document.getElementById(elementId);
        if (element) {
            const rect = element.getBoundingClientRect();
            return {
                width: rect.width,
                height: rect.height,
                top: rect.top,
                left: rect.left
            };
        }
        return null;
    } catch (error) {
        console.error('Get dimensions failed:', error);
        return null;
    }
};

/**
 * Show loading overlay
 */
window.showLoading = function (message = 'Loading...') {
    try {
        let overlay = document.getElementById('loading-overlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'loading-overlay';
            overlay.innerHTML = `
                <div class="loading-content">
                    <div class="spinner"></div>
                    <p>${message}</p>
                </div>
            `;
            document.body.appendChild(overlay);
        }
        overlay.style.display = 'flex';
    } catch (error) {
        console.error('Show loading failed:', error);
    }
};

/**
 * Hide loading overlay
 */
window.hideLoading = function () {
    try {
        const overlay = document.getElementById('loading-overlay');
        if (overlay) {
            overlay.style.display = 'none';
        }
    } catch (error) {
        console.error('Hide loading failed:', error);
    }
};

/**
 * Confirm dialog
 */
window.confirmDialog = async function (title, message) {
    return confirm(`${title}\n\n${message}`);
};

/**
 * Local storage helpers
 */
window.localStorageHelper = {
    get: function (key) {
        try {
            return localStorage.getItem(key);
        } catch (error) {
            console.error('LocalStorage get failed:', error);
            return null;
        }
    },
    set: function (key, value) {
        try {
            localStorage.setItem(key, value);
            return true;
        } catch (error) {
            console.error('LocalStorage set failed:', error);
            return false;
        }
    },
    remove: function (key) {
        try {
            localStorage.removeItem(key);
            return true;
        } catch (error) {
            console.error('LocalStorage remove failed:', error);
            return false;
        }
    },
    clear: function () {
        try {
            localStorage.clear();
            return true;
        } catch (error) {
            console.error('LocalStorage clear failed:', error);
            return false;
        }
    }
};

/**
 * Initialize enhanced features on page load
 */
document.addEventListener('DOMContentLoaded', function () {
    console.log('ResQLink Enhanced Features Loaded');
    
    // Add toast notification styles if not already present
    if (!document.getElementById('toast-styles')) {
        const style = document.createElement('style');
        style.id = 'toast-styles';
        style.textContent = `
            .toast-notification {
                position: fixed;
                top: 20px;
                right: 20px;
                min-width: 300px;
                max-width: 500px;
                background: white;
                border-radius: 8px;
                box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);
                z-index: 10000;
                opacity: 0;
                transform: translateX(400px);
                transition: all 0.3s ease;
            }
            
            .toast-notification.show {
                opacity: 1;
                transform: translateX(0);
            }
            
            .toast-content {
                display: flex;
                gap: 1rem;
                padding: 1rem;
                align-items: flex-start;
            }
            
            .toast-icon {
                width: 40px;
                height: 40px;
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 1.5rem;
                flex-shrink: 0;
            }
            
            .toast-info .toast-icon { background: #dbeafe; color: #2563eb; }
            .toast-success .toast-icon { background: #d1fae5; color: #059669; }
            .toast-warning .toast-icon { background: #fef3c7; color: #d97706; }
            .toast-error .toast-icon { background: #fee2e2; color: #dc2626; }
            .toast-critical .toast-icon { background: #fce7f3; color: #be185d; }
            
            .toast-body {
                flex: 1;
            }
            
            .toast-body strong {
                display: block;
                margin-bottom: 0.25rem;
                color: #111827;
            }
            
            .toast-body p {
                margin: 0;
                color: #6b7280;
                font-size: 0.875rem;
            }
            
            .toast-close {
                background: none;
                border: none;
                cursor: pointer;
                font-size: 1.25rem;
                color: #9ca3af;
                padding: 0;
                width: 24px;
                height: 24px;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            
            .toast-close:hover {
                color: #4b5563;
            }
            
            #loading-overlay {
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(0, 0, 0, 0.7);
                display: none;
                align-items: center;
                justify-content: center;
                z-index: 9999;
            }
            
            .loading-content {
                text-align: center;
                color: white;
            }
            
            .loading-content .spinner {
                width: 50px;
                height: 50px;
                border: 4px solid rgba(255, 255, 255, 0.3);
                border-top-color: white;
                border-radius: 50%;
                animation: spin 1s linear infinite;
                margin: 0 auto 1rem;
            }
            
            @keyframes spin {
                to { transform: rotate(360deg); }
            }
        `;
        document.head.appendChild(style);
    }
});

console.log('ResQLink Enhanced Features JavaScript Loaded Successfully');
