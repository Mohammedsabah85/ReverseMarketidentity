/**
 * مدير اللغات - نسخة محسّنة وموثوقة
 */

class LanguageManager {
    constructor() {
        this.currentLanguage = document.documentElement.lang || 'ar';
        this.supportedLanguages = ['ar', 'en', 'ku'];
        this.isChanging = false;

        console.log('✅ تم تحميل مدير اللغات:', this.currentLanguage);
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadKurdishStyleIfNeeded();
    }

    setupEventListeners() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.attachFormListeners());
        } else {
            this.attachFormListeners();
        }
    }

    attachFormListeners() {
        const forms = document.querySelectorAll('.language-form');
        console.log('📝 عدد نماذج اللغة:', forms.length);

        forms.forEach((form, index) => {
            const culture = form.querySelector('input[name="culture"]')?.value;
            console.log(`نموذج ${index + 1}: ${culture}`);

            form.addEventListener('submit', (e) => {
                e.preventDefault();
                e.stopPropagation();

                console.log('🔄 بدء تغيير اللغة إلى:', culture);
                this.changeLanguage(form);
            });
        });
    }

    loadKurdishStyleIfNeeded() {
        if (this.currentLanguage === 'ku') {
            if (!document.querySelector('link[href*="kurdish-rtl"]')) {
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = '/css/kurdish-rtl.css';
                document.head.appendChild(link);
                console.log('✅ تم تحميل ملف CSS الكردي');
            }
        }
    }

    async changeLanguage(form) {
        if (this.isChanging) {
            console.warn('⚠️ تغيير اللغة قيد التنفيذ بالفعل');
            return;
        }

        this.isChanging = true;

        const formData = new FormData(form);
        const culture = formData.get('culture');
        const returnUrl = formData.get('returnUrl') || window.location.href;

        console.log('📤 بيانات التغيير:', {
            culture,
            returnUrl,
            hasToken: !!formData.get('__RequestVerificationToken')
        });

        if (!this.supportedLanguages.includes(culture)) {
            console.error('❌ لغة غير مدعومة:', culture);
            this.isChanging = false;
            return;
        }

        if (culture === this.currentLanguage) {
            console.log('ℹ️ اللغة مختارة بالفعل');
            this.isChanging = false;
            return;
        }

        try {
            this.showLoading(form, culture);

            const response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin'
            });

            console.log('📥 الاستجابة:', response.status, response.ok);

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            // حفظ التفضيل
            try {
                localStorage.setItem('preferredLanguage', culture);
                console.log('💾 تم حفظ التفضيل');
            } catch (e) {
                console.warn('⚠️ لا يمكن حفظ في localStorage');
            }

            // إعادة التحميل
            console.log('🔄 إعادة تحميل الصفحة...');
            setTimeout(() => {
                window.location.href = returnUrl;
            }, 100);

        } catch (error) {
            console.error('❌ خطأ:', error);
            this.showError(this.getErrorMessage());
            this.hideLoading(form);
            this.isChanging = false;
        }
    }

    showLoading(form, culture) {
        const button = form.querySelector('button[type="submit"]');
        if (!button) return;

        button.disabled = true;
        button.dataset.original = button.innerHTML;

        const loadingText = {
            'ar': 'جاري التحميل...',
            'en': 'Loading...',
            'ku': 'لودکردن...'
        }[culture] || 'جاري التحميل...';

        button.innerHTML = `<i class="fas fa-spinner fa-spin me-2"></i>${loadingText}`;

        // تعطيل جميع الأزرار
        document.querySelectorAll('.language-form button').forEach(btn => {
            btn.disabled = true;
        });
    }

    hideLoading(form) {
        const button = form.querySelector('button[type="submit"]');
        if (button && button.dataset.original) {
            button.innerHTML = button.dataset.original;
            button.disabled = false;
            delete button.dataset.original;
        }

        document.querySelectorAll('.language-form button').forEach(btn => {
            btn.disabled = false;
        });
    }

    getErrorMessage() {
        const messages = {
            'ar': 'حدث خطأ في تغيير اللغة',
            'en': 'Error changing language',
            'ku': 'هەڵەیەک ڕوویدا'
        };
        return messages[this.currentLanguage] || messages['ar'];
    }

    showError(message) {
        document.querySelectorAll('.language-error').forEach(el => el.remove());

        const alert = document.createElement('div');
        alert.className = 'alert alert-danger alert-dismissible fade show position-fixed language-error';
        alert.style.cssText = 'top: 20px; right: 20px; left: 20px; z-index: 9999; max-width: 500px; margin: 0 auto;';
        alert.innerHTML = `
            <i class="fas fa-exclamation-circle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(alert);

        setTimeout(() => {
            if (alert.parentNode) alert.remove();
        }, 5000);
    }

    getCurrentLanguage() {
        return this.currentLanguage;
    }
}

// التهيئة
console.log('🚀 بدء تحميل مدير اللغات...');

let languageManager;

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        languageManager = new LanguageManager();
        window.languageManager = languageManager;
    });
} else {
    languageManager = new LanguageManager();
    window.languageManager = languageManager;
}

window.LanguageManager = LanguageManager;