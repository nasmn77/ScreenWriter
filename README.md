# ScreenWriter

> 🇬🇧 **English description is available below** — [Jump to English](#english)

تطبيق Windows للرسم والتوضيح فوق الشاشة بشكل كامل، مصمم للشرح أثناء الاجتماعات الافتراضية ومشاركة الشاشة. يدعم اللغتين العربية والإنجليزية مع تبديل فوري بدون إعادة تشغيل.

> Built with C# · WPF · .NET 10 · Windows

## 📥 التحميل

حمّل التطبيق من صفحة التحميل الرسمية:

### 👉 https://docs.naseralmadi.cloud/apps/ar/

---

## المميزات

- **طبقة شفافة فوق كل النوافذ** — يرسم فوق أي تطبيق دون إيقافه
- **مرئي في مشاركة الشاشة** — يظهر في Zoom وTeams وGoogle Meet وOBS
- **دعم اللغتين** — عربي وإنجليزي مع تبديل فوري من شريط الأدوات
- **أدوات رسم متعددة** — رسم حر، خط مستقيم، مستطيل، دائرة/بيضاوي، كتابة نص
- **8 ألوان** مع تحكم كامل في حجم القلم
- **ممحاة** وتراجع/إعادة كاملين (يشملان مسح الكل)
- **شريط أدوات عائم** ينزلق تلقائياً من أعلى يسار الشاشة
- **System Tray** للتشغيل في الخلفية
- **اختصارات لوحة المفاتيح** العالمية (تعمل من أي تطبيق)
- لا يسرق الـ Focus من التطبيقات الأخرى

---

## متطلبات التشغيل

| المتطلب | الإصدار |
|---|---|
| Windows | 10 أو أحدث |
| .NET Runtime | 10.0 |

لتحميل .NET Runtime:
```
https://dotnet.microsoft.com/download/dotnet/10.0
```

---

## تشغيل المشروع من المصدر

**المتطلبات:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11

```bash
git clone https://github.com/nasmn77/ScreenWriter.git
cd "ScreenWriter/ScreenWriter"
dotnet run
```

---

## طريقة الاستخدام

### 1. تشغيل التطبيق
عند التشغيل، تظهر أيقونة التطبيق في شريط المهام (System Tray) أسفل يمين الشاشة.

### 2. شريط الأدوات
يظهر شريط أدوات صغير في **أعلى يسار الشاشة**.

- **يختفي تلقائياً** بعد 2.5 ثانية من عدم التفاعل (ينزلق للأعلى)
- **يظهر مجدداً** عند تمرير الماوس على الزاوية العلوية اليسرى

### 3. تفعيل وضع الرسم
انقر زر **"رسم"** في شريط الأدوات أو اضغط `Ctrl + Alt + D`

- الدائرة الخضراء = وضع الرسم نشط ✅
- الدائرة الرمادية = وضع المرور (click-through) ⭕

### 4. الرسم
اختر الأداة المطلوبة من شريط الأدوات ثم ارسم مباشرة على الشاشة.

---

## شريط الأدوات

```
[ رسم/إيقاف ] | [ ● ● ● ● ● ● ● ● ] | [ ━ حجم ] | [ 🖊 ╱ ▭ ⬭ T ⌫ ] | [ ↩ ↪ 🗑 ] | [ ✕ ]
```

| العنصر | الوصف |
|---|---|
| **رسم / إيقاف** | تفعيل أو إيقاف وضع الرسم |
| **● الألوان** | أحمر، برتقالي، أصفر، أخضر، أزرق، بنفسجي، أبيض، أسود |
| **━ الحجم** | شريط تمرير لضبط سماكة القلم (2–30) |
| **🖊 رسم حر** | خطوط حرة سلسة |
| **╱ خط** | خط مستقيم بين نقطتين |
| **▭ مستطيل** | مستطيل بالسحب |
| **⬭ دائرة** | دائرة أو شكل بيضاوي بالسحب |
| **T نص** | كتابة نص بخط Arial على الشاشة — اضغط Enter لسطر جديد، Esc للإلغاء |
| **⌫ ممحاة** | محو الرسومات والأشكال والنصوص |
| **↩ تراجع** | تراجع عن آخر خطوة |
| **↪ إعادة** | إعادة الخطوة الملغاة |
| **🗑 مسح الكل** | مسح جميع الرسومات مع رسالة تأكيد |
| **✕ إغلاق** | إغلاق التطبيق نهائياً |

---

## اختصارات لوحة المفاتيح

| الاختصار | الوظيفة |
|---|---|
| `Ctrl + Alt + D` | تفعيل / إيقاف وضع الرسم |
| `Ctrl + Alt + C` | مسح جميع الرسومات (مع تأكيد) |

---

## قائمة System Tray

انقر بزر الفأرة الأيمن على أيقونة التطبيق في شريط المهام:

| الخيار | الوظيفة |
|---|---|
| تفعيل / إيقاف الرسم | نفس `Ctrl + Alt + D` |
| إظهار / إخفاء شريط الأدوات | إخفاء الشريط يدوياً |
| مسح الكل | مسح جميع الرسومات |
| خروج | إغلاق التطبيق |

---

## ملاحظات مهمة للاجتماعات

> ⚠️ **لظهور الرسومات في مشاركة الشاشة** يجب مشاركة **الشاشة كاملة** وليس نافذة تطبيق محدد.

| طريقة المشاركة | تظهر الرسومات؟ |
|---|---|
| مشاركة الشاشة كاملة ✅ | نعم |
| مشاركة نافذة تطبيق محدد ❌ | لا |

---

## هيكل المشروع

```
ScreenWriter/
├── App.xaml / App.xaml.cs          # نقطة الدخول وربط المكونات
├── Models/
│   └── DrawingSettings.cs          # لوحة الألوان، enum أدوات الرسم
├── Resources/
│   ├── Strings.ar.xaml             # نصوص اللغة العربية
│   └── Strings.en.xaml             # نصوص اللغة الإنجليزية
├── Windows/
│   ├── OverlayWindow.xaml(.cs)     # النافذة الشفافة الرئيسية + InkCanvas
│   ├── ToolbarWindow.xaml(.cs)     # شريط الأدوات العائم
│   ├── AboutWindow.xaml(.cs)       # نافذة "عن البرنامج"
│   └── SplashWindow.xaml(.cs)      # شاشة البداية
└── Services/
    ├── HotkeyService.cs            # اختصارات لوحة المفاتيح العالمية (Win32)
    ├── TrayService.cs              # أيقونة System Tray
    └── LocalizationService.cs      # إدارة اللغة والتبديل الفوري
```

---

## التفاصيل التقنية

| التقنية | التفصيل |
|---|---|
| **WPF InkCanvas** | رسم القلم الحر بضغط وسلاسة |
| **WS_EX_TRANSPARENT** | تمرير نقرات الماوس للنوافذ الخلفية عند إيقاف الرسم |
| **WS_EX_NOACTIVATE + MA_NOACTIVATE** | منع الـ overlay من سرقة الـ focus |
| **WS_EX_LAYERED** | دعم الشفافية الكاملة |
| **RegisterHotKey (Win32)** | اختصارات عالمية تعمل من أي تطبيق |
| **DWM Compositing** | الرسومات مرئية في screen capture تلقائياً |

---

## الترخيص

MIT License — حر الاستخدام والتعديل والتوزيع.

---

---

<a name="english"></a>

# ScreenWriter

A Windows app for drawing and annotating directly on screen, designed for explaining ideas during virtual meetings and screen sharing. Supports Arabic and English with instant switching — no restart needed.

> Built with C# · WPF · .NET 10 · Windows

## 📥 Download

Download the app from the official page:

### 👉 https://docs.naseralmadi.cloud/apps/en/

---

## Features

- **Transparent overlay** — draws over any app without disabling it
- **Visible in screen share** — works with Zoom, Teams, Google Meet, and OBS
- **Bilingual support** — Arabic & English with instant toggle from the toolbar
- **Multiple drawing tools** — free draw, straight line, rectangle, circle/ellipse, text
- **8 colors** with full pen size control
- **Eraser** and full undo/redo (including clear all)
- **Floating toolbar** — auto-slides in from the top-left corner of the screen
- **System Tray** for background operation
- **Global hotkeys** — work from any app
- Does not steal focus from other applications

---

## Requirements

| Requirement | Version |
|---|---|
| Windows | 10 or later |
| .NET Runtime | 10.0 |

---

## Run from Source

**Prerequisites:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11

```bash
git clone https://github.com/nasmn77/ScreenWriter.git
cd "ScreenWriter/ScreenWriter"
dotnet run
```

---

## Usage

### 1. Launch the app
The app icon appears in the System Tray at the bottom-right of the screen.

### 2. Toolbar
A small toolbar appears at the **top-left of the screen**.

- **Auto-hides** after 2.5 seconds of inactivity (slides up)
- **Reappears** when you hover over the top-left corner

### 3. Toggle drawing mode
Click the **"Draw"** button in the toolbar or press `Ctrl + Alt + D`

- Green dot = drawing mode active ✅
- Gray dot = click-through mode ⭕

### 4. Draw
Select a tool from the toolbar and draw directly on the screen.

---

## Toolbar

```
[ Draw/Stop ] | [ ● ● ● ● ● ● ● ● ] | [ ━ Size ] | [ 🖊 ╱ ▭ ⬭ T ⌫ ] | [ ↩ ↪ 🗑 ] | [ EN/ع ] | [ ✕ ]
```

| Element | Description |
|---|---|
| **Draw / Stop** | Toggle drawing mode on or off |
| **● Colors** | Red, Orange, Yellow, Green, Blue, Purple, White, Black |
| **━ Size** | Slider to adjust pen thickness (2–30) |
| **🖊 Free Draw** | Smooth freehand lines |
| **╱ Line** | Straight line between two points |
| **▭ Rectangle** | Rectangle by dragging |
| **⬭ Circle** | Circle or ellipse by dragging |
| **T Text** | Write text on screen — Enter for new line, Esc to cancel |
| **⌫ Eraser** | Erase drawings, shapes, and text |
| **↩ Undo** | Undo last action |
| **↪ Redo** | Redo undone action |
| **🗑 Clear All** | Clear all drawings with confirmation |
| **EN/ع** | Toggle language between English and Arabic |
| **✕ Close** | Close the application |

---

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl + Alt + D` | Toggle drawing mode on / off |
| `Ctrl + Alt + C` | Clear all drawings (with confirmation) |

---

## System Tray Menu

Right-click the app icon in the taskbar:

| Option | Action |
|---|---|
| Toggle Draw | Same as `Ctrl + Alt + D` |
| Show / Hide Toolbar | Manually hide the toolbar |
| Clear All | Clear all drawings |
| Exit | Close the application |

---

## Important Note for Meetings

> ⚠️ **For drawings to appear in screen share**, you must share the **entire screen**, not a specific application window.

| Share method | Drawings visible? |
|---|---|
| Full screen share ✅ | Yes |
| Application window share ❌ | No |

---

## Project Structure

```
ScreenWriter/
├── App.xaml / App.xaml.cs          # Entry point and component wiring
├── Models/
│   └── DrawingSettings.cs          # Color palette, drawing tools enum
├── Resources/
│   ├── Strings.ar.xaml             # Arabic strings
│   └── Strings.en.xaml             # English strings
├── Windows/
│   ├── OverlayWindow.xaml(.cs)     # Main transparent overlay + InkCanvas
│   ├── ToolbarWindow.xaml(.cs)     # Floating toolbar
│   ├── AboutWindow.xaml(.cs)       # About window
│   └── SplashWindow.xaml(.cs)      # Splash screen
└── Services/
    ├── HotkeyService.cs            # Global hotkeys (Win32)
    ├── TrayService.cs              # System Tray icon
    └── LocalizationService.cs      # Language management and instant switching
```

---

## Technical Details

| Technology | Detail |
|---|---|
| **WPF InkCanvas** | Pressure-sensitive freehand drawing |
| **WS_EX_TRANSPARENT** | Mouse click-through when drawing is off |
| **WS_EX_NOACTIVATE + MA_NOACTIVATE** | Prevents overlay from stealing focus |
| **WS_EX_LAYERED** | Full transparency support |
| **RegisterHotKey (Win32)** | Global hotkeys that work from any app |
| **DWM Compositing** | Drawings are visible in screen capture automatically |

---

## License

MIT License — free to use, modify, and distribute.
