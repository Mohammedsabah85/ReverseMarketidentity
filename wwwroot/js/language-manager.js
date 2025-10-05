/**
 * Language Manager - إدارة اللغات المتعددة
 * يتعامل مع تبديل اللغات وحفظ التفضيلات
 */

class LanguageManager {
    constructor() {
        this.currentLanguage = document.documentElement.lang || 'ar';
        this.supportedLanguages = ['ar', 'en', 'ku'];
        this.init();
    }

    init() {
        this.attachEventListeners();
        this.loadKurdishCssIfNeeded();
        this.updateUIBasedOnLanguage();
    }

    /**
     * ربط مستمعي الأحداث
     */
    attachEventListeners() {
        // استماع لتغيير اللغة
        document.addEventListener('DOMContentLoaded', () => {
            const languageForms = document.querySelectorAll('.language-form');

            languageForms.forEach(form => {
                form.addEventListener('submit', (e) => {
                    e.preventDefault();
                    this.changeLanguage(form);
                });
            });
        });
    }

    /**
     * تحميل ملف CSS الخاص بالكردية عند الحاجة
     */
    loadKurdishCssIfNeeded() {
        if (this.currentLanguage === 'ku') {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = '/css/kurdish-rtl.css';
            document.head.appendChild(link);
        }
    }

    /**
     * تحديث الواجهة بناءً على اللغة
     */
    updateUIBasedOnLanguage() {
        const direction = this.getDirection(this.currentLanguage);
        document.documentElement.dir = direction;

        // تحديث Bootstrap إذا لزم الأمر
        if (direction === 'rtl' && !this.isBootstrapRTLLoaded()) {
            this.loadBootstrapRTL();
        }
    }

    /**
     * الحصول على اتجاه اللغة
     */
    getDirection(language) {
        const rtlLanguages = ['ar', 'ku'];
        return rtlLanguages.includes(language) ? 'rtl' : 'ltr';
    }

    /**
     * التحقق من تحميل Bootstrap RTL
     */
    isBootstrapRTLLoaded() {
        return Array.from(document.styleSheets).some(sheet =>
            sheet.href && sheet.href.includes('bootstrap.rtl')
        );
    }

    /**
     * تحميل Bootstrap RTL
     */
    loadBootstrapRTL() {
        // إزالة Bootstrap LTR
        const bootstrapLinks = Array.from(document.querySelectorAll('link[href*="bootstrap"]'));
        bootstrapLinks.forEach(link => {
            if (!link.href.includes('rtl')) {
                link.remove();
            }
        });

        // إضافة Bootstrap RTL
        const rtlLink = document.createElement('link');
        rtlLink.rel = 'stylesheet';
        rtlLink.href = 'https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.rtl.min.css';
        document.head.appendChild(rtlLink);
    }

    /**
     * تغيير اللغة
     */
    async changeLanguage(form) {
        const formData = new FormData(form);
        const culture = formData.get('culture');
        const returnUrl = formData.get('returnUrl') || window.location.href;

        if (!this.supportedLanguages.includes(culture)) {
            console.error('اللغة غير مدعومة:', culture);
            return;
        }

        try {
            // تحديث UI لإظهار التحميل
            this.showLoadingState(form);

            // إرسال الطلب
            const response = await fetch(form.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (response.ok) {
                // حفظ في localStorage
                localStorage.setItem('preferredLanguage', culture);

                // إعادة تحميل الصفحة
                window.location.href = returnUrl;
            } else {
                throw new Error(`HTTP ${response.status}`);
            }
        } catch (error) {
            console.error('خطأ في تغيير اللغة:', error);
            this.showError('حدث خطأ في تغيير اللغة');
            this.resetLoadingState(form);
        }
    }

    /**
     * إظهار حالة التحميل
     */
    showLoadingState(form) {
        const button = form.querySelector('button[type="submit"]');
        if (button) {
            button.disabled = true;
            button.dataset.originalHtml = button.innerHTML;

            const loadingText = this.getLoadingText();
            button.innerHTML = `<i class="fas fa-spinner fa-spin me-2"></i>${loadingText}`;
        }

        // إغلاق القائمة المنسدلة
        const dropdown = bootstrap.Dropdown.getInstance(
            document.getElementById('languageMenu')
        );
        if (dropdown) {
            dropdown.hide();
        }
    }

    /**
     * إعادة تعيين حالة التحميل
     */
    resetLoadingState(form) {
        const button = form.querySelector('button[type="submit"]');
        if (button && button.dataset.originalHtml) {
            button.innerHTML = button.dataset.originalHtml;
            button.disabled = false;
            delete button.dataset.originalHtml;
        }
    }

    /**
     * الحصول على نص التحميل
     */
    getLoadingText() {
        const loadingTexts = {
            'ar': 'جاري التحميل...',
            'en': 'Loading...',
            'ku': 'لودکردن...'
        };
        return loadingTexts[this.currentLanguage] || 'Loading...';
    }

    /**
     * عرض رسالة خطأ
     */
    showError(message) {
        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-danger alert-dismissible fade show position-fixed';
        alertDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; max-width: 400px;';
        alertDiv.innerHTML = `
            <i class="fas fa-exclamation-circle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(alertDiv);

        setTimeout(() => {
            alertDiv.remove();
        }, 5000);
    }

    /**
     * الحصول على اللغة الحالية
     */
    getCurrentLanguage() {
        return this.currentLanguage;
    }

    /**
     * التحقق من دعم اللغة
     */
    isLanguageSupported(language) {
        return this.supportedLanguages.includes(language);
    }

    /**
     * الحصول على اسم اللغة
     */
    getLanguageName(language) {
        const names = {
            'ar': 'العربية',
            'en': 'English',
            'ku': 'کوردی'
        };
        return names[language] || language;
    }

    /**
     * تطبيق اللغة المفضلة تلقائياً
     */
    autoApplyPreferredLanguage() {
        const preferred = localStorage.getItem('preferredLanguage');

        if (preferred &&
            preferred !== this.currentLanguage &&
            this.isLanguageSupported(preferred)) {

            const form = document.querySelector(
                `.language-form input[value="${preferred}"]`
            )?.closest('form');

            if (form) {
                setTimeout(() => {
                    form.dispatchEvent(new Event('submit', {
                        bubbles: true,
                        cancelable: true
                    }));
                }, 200);
            }
        }
    }
}

// تهيئة مدير اللغات
const languageManager = new LanguageManager();

// تصدير للاستخدام العام
window.LanguageManager = LanguageManager;
window.languageManager = languageManager;

// تطبيق اللغة المفضلة عند تحميل الصفحة
document.addEventListener('DOMContentLoaded', () => {
    // إعطاء وقت للصفحة للتحميل قبل تطبيق اللغة
    setTimeout(() => {
        languageManager.autoApplyPreferredLanguage();
    }, 500);
});