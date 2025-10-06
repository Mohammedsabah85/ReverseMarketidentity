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

        // ✅ إنشاء FormData هنا
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
                credentials: 'same-origin',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            console.log('📥 الاستجابة:', response.status, response.ok);

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const data = await response.json();
            console.log('📊 البيانات المستلمة:', data);

            if (data.success) {
                console.log('✅ تم تغيير اللغة بنجاح');

                // إعادة التحميل باستخدام URL المعاد من السيرفر
                window.location.href = data.redirectUrl || returnUrl;
            } else {
                throw new Error(data.message || 'فشل تغيير اللغة');
            }

        } catch (error) {
            console.error('❌ خطأ:', error);
            this.showError(this.getErrorMessage(error.message));
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

        // تعطيل جميع أزرار اللغة
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

        // إعادة تفعيل جميع الأزرار
        document.querySelectorAll('.language-form button').forEach(btn => {
            btn.disabled = false;
        });
    }

    getErrorMessage(specificError = '') {
        const messages = {
            'ar': specificError || 'حدث خطأ في تغيير اللغة',
            'en': specificError || 'Error changing language',
            'ku': specificError || 'هەڵەیەک ڕوویدا'
        };
        return messages[this.currentLanguage] || messages['ar'];
    }

    showError(message) {
        // إزالة التنبيهات السابقة
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
        console.log('✅ تم تهيئة مدير اللغات');
    });
} else {
    languageManager = new LanguageManager();
    window.languageManager = languageManager;
    console.log('✅ تم تهيئة مدير اللغات');
}

window.LanguageManager = LanguageManager;