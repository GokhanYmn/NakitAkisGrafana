window.downloadFile = async (url, jsonData) => {
    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: jsonData
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        // Content-Disposition header'ından dosya adını al
        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = 'nakit-akis-raporu';

        if (contentDisposition) {
            const filenameMatch = contentDisposition.match(/filename="(.+)"/);
            if (filenameMatch) {
                filename = filenameMatch[1];
            }
        }

        // Blob oluştur ve indir
        const blob = await response.blob();
        const downloadUrl = window.URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = downloadUrl;
        a.download = filename;

        document.body.appendChild(a);
        a.click();

        // Cleanup
        window.URL.revokeObjectURL(downloadUrl);
        document.body.removeChild(a);

        // Success message
        showNotification('Dosya başarıyla indirildi!', 'success');

    } catch (error) {
        console.error('Download failed:', error);
        showNotification('İndirme sırasında hata oluştu: ' + error.message, 'error');
    }
};

window.openHtmlInNewTab = (htmlContent) => {
    try {
        const newWindow = window.open('', '_blank');
        if (newWindow) {
            newWindow.document.write(htmlContent);
            newWindow.document.close();
            showNotification('HTML raporu yeni sekmede açıldı!', 'success');
        } else {
            // Popup engellenmiş
            showNotification('Popup engellendi. Lütfen popup engelleyiciyi devre dışı bırakın.', 'warning');
        }
    } catch (error) {
        console.error('HTML open failed:', error);
        showNotification('HTML açılırken hata oluştu: ' + error.message, 'error');
    }
};

window.showNotification = (message, type = 'info') => {
    // Bootstrap toast veya basit alert
    const alertClass = {
        'success': 'alert-success',
        'error': 'alert-danger',
        'warning': 'alert-warning',
        'info': 'alert-info'
    }[type] || 'alert-info';

    // Notification container oluştur (yoksa)
    let container = document.getElementById('notification-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'notification-container';
        container.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            max-width: 400px;
        `;
        document.body.appendChild(container);
    }

    // Alert oluştur
    const alert = document.createElement('div');
    alert.className = `alert ${alertClass} alert-dismissible fade show`;
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    container.appendChild(alert);

    // 5 saniye sonra kaldır
    setTimeout(() => {
        if (alert.parentNode) {
            alert.parentNode.removeChild(alert);
        }
    }, 5000);
};