# ScreenWriter

> 🇬🇧 **English description is available below** — [Jump to English](#english)

تطبيق Windows للرسم والتوضيح فوق الشاشة بشكل كامل، مصمم للشرح أثناء الاجتماعات الافتراضية ومشاركة الشاشة. يدعم اللغتين العربية والإنجليزية مع تبديل فوري بدون إعادة تشغيل.

> Built with C# · WPF · .NET 10 · Windows

## 📥 التحميل

حمّل التطبيق من **متجر مايكروسوفت** — تثبيت آمن وتحديثات تلقائية:

### 👉 https://apps.microsoft.com/detail/9nf25dzqxlct

صفحة التطبيق: https://nasmn77.github.io/ScreenWriter/ar/

---

## المميزات

- **طبقة شفافة فوق كل النوافذ** — يرسم فوق أي تطبيق دون إيقافه
- **مرئي في مشاركة الشاشة** — يظهر في Zoom وTeams وGoogle Meet وOBS
- **دعم اللغتين** — عربي وإنجليزي مع تبديل فوري من شريط الأدوات
- **أدوات رسم متعددة** — رسم حر، سهم، خط مستقيم، مستطيل، دائرة/بيضاوي، كتابة نص
- **أداة التحديد** — تحديد أي كائن ونقله وتغيير حجمه، مع إمكانية تعديل النص بالنقر المزدوج
- **تغيير لون وسماكة الكائن المحدد** — مباشرة من شريط الأدوات بعد التحديد
- **8 ألوان** مع تحكم كامل في حجم القلم
- **ممحاة** تمسح الكائن كاملاً بلمسة واحدة
- **تراجع/إعادة** كاملين يشملان جميع العمليات
- **سبورة بيضاء وسوداء للتعليم** — لكل سبورة رسوماتها وسجل تراجعها، مع ألوان قلم مناسبة لخلفيتها
- **شريط الأدوات مخفي في البث** — يراه المستخدم ولا يظهر للمشاهدين (Windows 10 2004 أو أحدث)
- **شريط أدوات عائم** ينزلق تلقائياً — يمكن تبديل موضعه بين يسار ويمين الشاشة
- **System Tray** للتشغيل في الخلفية
- **اختصارات لوحة المفاتيح** العالمية (تعمل من أي تطبيق)
- لا يسرق الـ Focus من التطبيقات الأخرى

---

## متطلبات التشغيل

| المتطلب | الإصدار |
|---|---|
| Windows | 10 (الإصدار 1809) أو أحدث — 64-bit |

لا يحتاج تثبيت .NET؛ نسخة المتجر تتضمن كل ما يلزم للتشغيل.

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

### السبورة
افتح قائمة **السبورة** (بجانب زر الرسم) واختر **سبورة بيضاء** أو **سبورة سوداء**، أو اضغط `Ctrl + Alt + W` / `Ctrl + Alt + B`.

- تغطي السبورة الشاشة التي عليها مؤشر الفأرة، وتبقى باقي الشاشات طبيعية
- لكل وضع (الشاشة، البيضاء، السوداء) رسوماته وسجل تراجعه ولون قلمه الخاص
- يبقى شريط الأدوات ظاهراً طوال الوقت على السبورة
- للعودة للشاشة: اختر **الشاشة (شفاف)** من القائمة، أو اضغط `Ctrl + Alt + T`، أو اختصار السبورة نفسها مرة أخرى
- رسومات السبورة لا تُحفظ بعد إغلاق التطبيق، ويظهر تنبيه عند الخروج إذا كانت تحتوي على رسومات

### 4. الرسم
اختر الأداة المطلوبة من شريط الأدوات ثم ارسم مباشرة على الشاشة.

### 5. التحديد والتعديل
اختر أداة **التحديد** ثم:
- **انقر** على أي كائن لتحديده
- **اسحب** الكائن لنقله
- **اسحب أحد المقابض** لتغيير حجمه
- **انقر مزدوجاً** على نص لتعديله
- غيّر **اللون أو السماكة** من شريط الأدوات بعد التحديد

---

## شريط الأدوات

```
[ رسم/إيقاف ▢▾ ] | [ ● ● ● ● ● ● ● ● ] | [ ━ حجم ] | [ 🖊 ⇒ ╱ ▭ ⬭ T ⬚ ⌫ ] | [ ↩ ↪ 🗑 ] | [ ⓘ ⇌ EN/ع ✕ ]
```

| العنصر | الوصف |
|---|---|
| **رسم / إيقاف** | تفعيل أو إيقاف وضع الرسم |
| **▢▾ السبورة** | قائمة لاختيار الوضع: الشاشة (شفاف)، سبورة بيضاء، سبورة سوداء |
| **● الألوان** | أحمر، برتقالي، أصفر، أخضر، أزرق، بنفسجي، أبيض، أسود |
| **━ الحجم** | شريط تمرير لضبط سماكة القلم (2–30) |
| **🖊 رسم حر** | خطوط حرة سلسة |
| **⇒ سهم** | سهم مع رأس سهم تلقائي |
| **╱ خط** | خط مستقيم بين نقطتين |
| **▭ مستطيل** | مستطيل بالسحب |
| **⬭ دائرة** | دائرة أو شكل بيضاوي بالسحب |
| **T نص** | كتابة نص بخط Arial على الشاشة — اضغط Enter لسطر جديد، Esc للإلغاء |
| **⬚ تحديد** | تحديد الكائنات ونقلها وتغيير حجمها |
| **⌫ ممحاة** | محو الكائن بالكامل بلمسة واحدة |
| **↩ تراجع** | تراجع عن آخر خطوة |
| **↪ إعادة** | إعادة الخطوة الملغاة |
| **🗑 مسح الكل** | مسح جميع الرسومات مع رسالة تأكيد |
| **ⓘ عن البرنامج** | رقم الإصدار، التحقق من التحديثات، وزر التقييم والمراجعة في المتجر |
| **⇌ موضع الشريط** | تبديل موضع شريط الأدوات بين يسار ويمين الشاشة |
| **EN/ع اللغة** | تبديل فوري بين العربية والإنجليزية |
| **✕ إغلاق** | إغلاق التطبيق نهائياً |

---

## اختصارات لوحة المفاتيح

| الاختصار | الوظيفة |
|---|---|
| `Ctrl + Alt + D` | تفعيل / إيقاف وضع الرسم |
| `Ctrl + Alt + C` | مسح جميع الرسومات (مع تأكيد) |
| `Ctrl + Z` | تراجع (أثناء وضع الرسم) |
| `Ctrl + Y` | إعادة (أثناء وضع الرسم) |
| `Ctrl + Alt + W` | سبورة بيضاء |
| `Ctrl + Alt + B` | سبورة سوداء |
| `Ctrl + Alt + T` | العودة للشاشة (الوضع الشفاف) |

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
│   └── DrawingSettings.cs          # لوحات الألوان لكل وضع، أدوات الرسم وأوضاع السبورة
├── Resources/
│   ├── Strings.ar.xaml             # نصوص اللغة العربية
│   └── Strings.en.xaml             # نصوص اللغة الإنجليزية
├── Windows/
│   ├── OverlayWindow.xaml(.cs)     # النافذة الشفافة الرئيسية + InkCanvas
│   ├── ToolbarWindow.xaml(.cs)     # شريط الأدوات العائم
│   ├── AboutWindow.xaml(.cs)       # نافذة "عن البرنامج" (التحديثات + التقييم)
│   ├── UpdateDialog.xaml(.cs)      # نافذة التحديث المتوفر
│   └── SplashWindow.xaml(.cs)      # شاشة البداية
└── Services/
    ├── HotkeyService.cs            # اختصارات لوحة المفاتيح العالمية (Win32)
    ├── CaptureExclusion.cs         # إخفاء شريط الأدوات من مشاركة الشاشة
    ├── TrayService.cs              # أيقونة System Tray
    ├── LocalizationService.cs      # إدارة اللغة والتبديل الفوري
    ├── SettingsService.cs          # حفظ واستعادة إعدادات المستخدم
    └── UpdateService.cs            # التحديثات وفتح صفحة التطبيق والتقييم في المتجر
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
| **SetWindowDisplayAffinity** | إخفاء شريط الأدوات وقوائمه من البث (`WDA_EXCLUDEFROMCAPTURE`) |

---

## سجل التغييرات

### v2.1.0
- **وضع السبورة للتعليم:** سبورة بيضاء وسوداء من قائمة جديدة في شريط الأدوات، أو بالاختصارات `Ctrl+Alt+W` / `Ctrl+Alt+B` / `Ctrl+Alt+T`
- لكل وضع لوحة رسم مستقلة بسجل تراجع ولون قلم خاص، وتتبدل ألوان القلم لتكون واضحة على كل سبورة
- السبورة تغطي الشاشة التي عليها مؤشر الفأرة فقط
- **شريط الأدوات لا يظهر في البث** (Zoom، Teams، OBS) ويبقى ظاهراً للمستخدم
- تنبيه عند الإغلاق إذا كانت السبورة تحتوي على رسومات
- إصلاح: `Ctrl+Z` و`Ctrl+Y` لم تعودا تعطّلان التراجع في البرامج الأخرى عند إيقاف الرسم
- إصلاحات في التراجع والإعادة: الممحاة، وتحريك وتغيير حجم السهم والخط، وحجم خط النص، وتعديل النص

### v2.0.1
- إضافة زر **التقييم والمراجعة** في نافذة «عن البرنامج» يفتح نموذج التقييم في المتجر مباشرة

### v2.0.0
- إضافة أداة **التحديد / النقل / تغيير الحجم** لجميع الكائنات (أشكال، نصوص، رسم حر)
- دعم **تعديل النص** بالنقر المزدوج في وضع التحديد
- **تغيير لون وسماكة** الكائن المحدد مباشرة من شريط الأدوات
- مزامنة **شريط السماكة** تلقائياً عند تحديد كائن
- إطار وهمي عند **مرور الماوس** فوق أي كائن في وضع التحديد أو الممحاة
- الممحاة تمسح **الكائن كاملاً** بلمسة واحدة
- تغيير شكل **مؤشر الماوس** حسب المقبض عند تغيير الحجم

### v1.0.1
- إصلاح مشكلة تشغيل متعدد للتطبيق
- تحسينات على خدمة التحديث التلقائي

### v1.0.0
- الإصدار الأول

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

Get the app from the **Microsoft Store** — secure install and automatic updates:

### 👉 https://apps.microsoft.com/detail/9nf25dzqxlct

App page: https://nasmn77.github.io/ScreenWriter/en/

---

## Features

- **Transparent overlay** — draws over any app without disabling it
- **Visible in screen share** — works with Zoom, Teams, Google Meet, and OBS
- **Bilingual support** — Arabic & English with instant toggle from the toolbar
- **Multiple drawing tools** — free draw, arrow, straight line, rectangle, circle/ellipse, text
- **Select tool** — select any object to move or resize it, double-click text to edit it
- **Change color and thickness of selected objects** — directly from the toolbar after selecting
- **8 colors** with full pen size control
- **Eraser** that removes the entire object in one touch
- **Full undo/redo** covering all operations
- **Whiteboard and blackboard for teaching** — each board keeps its own drawings and undo history, with pen colors suited to its background
- **Toolbar hidden from screen sharing** — you see it, viewers don't (Windows 10 2004 or later)
- **Floating toolbar** — auto-slides in; position can be toggled between left and right
- **System Tray** for background operation
- **Global hotkeys** — work from any app
- Does not steal focus from other applications

---

## Requirements

| Requirement | Version |
|---|---|
| Windows | 10 (version 1809) or later — 64-bit |

No .NET install needed; the Store version includes everything required to run.

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

### Board
Open the **Board** menu (next to the Draw button) and choose **Whiteboard** or **Blackboard**, or press `Ctrl + Alt + W` / `Ctrl + Alt + B`.

- The board covers the monitor under the mouse cursor; other monitors stay as they are
- Each mode (screen, whiteboard, blackboard) has its own drawings, undo history, and pen color
- The toolbar stays visible all the time on a board
- To return to the screen: choose **Screen (transparent)** from the menu, press `Ctrl + Alt + T`, or press the board's own shortcut again
- Board drawings are not saved after closing; you get a warning on exit if a board has drawings

### 4. Draw
Select a tool from the toolbar and draw directly on the screen.

### 5. Select and edit
Choose the **Select** tool then:
- **Click** any object to select it
- **Drag** to move it
- **Drag a handle** to resize it
- **Double-click** text to edit it
- Change **color or thickness** from the toolbar while an object is selected

---

## Toolbar

```
[ Draw/Stop ▢▾ ] | [ ● ● ● ● ● ● ● ● ] | [ ━ Size ] | [ 🖊 ⇒ ╱ ▭ ⬭ T ⬚ ⌫ ] | [ ↩ ↪ 🗑 ] | [ ⓘ ⇌ EN/ع ✕ ]
```

| Element | Description |
|---|---|
| **Draw / Stop** | Toggle drawing mode on or off |
| **▢▾ Board** | Menu to choose the mode: Screen (transparent), Whiteboard, Blackboard |
| **● Colors** | Red, Orange, Yellow, Green, Blue, Purple, White, Black |
| **━ Size** | Slider to adjust pen thickness (2–30) |
| **🖊 Free Draw** | Smooth freehand lines |
| **⇒ Arrow** | Arrow with automatic arrowhead |
| **╱ Line** | Straight line between two points |
| **▭ Rectangle** | Rectangle by dragging |
| **⬭ Circle** | Circle or ellipse by dragging |
| **T Text** | Write text on screen — Enter for new line, Esc to cancel |
| **⬚ Select** | Select, move, and resize any object |
| **⌫ Eraser** | Remove the entire object in one touch |
| **↩ Undo** | Undo last action |
| **↪ Redo** | Redo undone action |
| **🗑 Clear All** | Clear all drawings with confirmation |
| **ⓘ About** | Version info, check for updates, and Rate & review in the Store |
| **⇌ Move Toolbar** | Toggle toolbar position between left and right |
| **EN/ع** | Toggle language between English and Arabic |
| **✕ Close** | Close the application |

---

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl + Alt + D` | Toggle drawing mode on / off |
| `Ctrl + Alt + C` | Clear all drawings (with confirmation) |
| `Ctrl + Z` | Undo (while drawing mode is active) |
| `Ctrl + Y` | Redo (while drawing mode is active) |
| `Ctrl + Alt + W` | Whiteboard |
| `Ctrl + Alt + B` | Blackboard |
| `Ctrl + Alt + T` | Back to the screen (transparent mode) |

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
│   └── DrawingSettings.cs          # Color palettes per mode, drawing tools and board modes
├── Resources/
│   ├── Strings.ar.xaml             # Arabic strings
│   └── Strings.en.xaml             # English strings
├── Windows/
│   ├── OverlayWindow.xaml(.cs)     # Main transparent overlay + InkCanvas
│   ├── ToolbarWindow.xaml(.cs)     # Floating toolbar
│   ├── AboutWindow.xaml(.cs)       # About window (updates + rating)
│   ├── UpdateDialog.xaml(.cs)      # Update-available dialog
│   └── SplashWindow.xaml(.cs)      # Splash screen
└── Services/
    ├── HotkeyService.cs            # Global hotkeys (Win32)
    ├── CaptureExclusion.cs         # Hides the toolbar from screen sharing
    ├── TrayService.cs              # System Tray icon
    ├── LocalizationService.cs      # Language management and instant switching
    ├── SettingsService.cs          # Save and restore user settings
    └── UpdateService.cs            # Updates and opening the Store listing / review form
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
| **SetWindowDisplayAffinity** | Hides the toolbar and its menus from capture (`WDA_EXCLUDEFROMCAPTURE`) |

---

## Changelog

### v2.1.0
- **Board mode for teaching:** whiteboard and blackboard from a new toolbar menu, or with `Ctrl+Alt+W` / `Ctrl+Alt+B` / `Ctrl+Alt+T`
- Each mode has its own canvas, undo history and pen color; pen colors switch to ones that are visible on each board
- A board covers only the monitor under the mouse cursor
- **The toolbar is hidden from screen sharing** (Zoom, Teams, OBS) while staying visible to you
- Warning on exit if a board has drawings
- Fix: `Ctrl+Z` and `Ctrl+Y` no longer block undo in other apps while drawing is off
- Undo/redo fixes: eraser, moving and resizing arrows and lines, text font size, and text editing

### v2.0.1
- Added a **Rate & review** button in the About window that opens the Store review form directly

### v2.0.0
- Added **Select / Move / Resize** tool for all object types (shapes, text, freehand strokes)
- **Double-click to edit** text in select mode
- **Change color and thickness** of any selected object from the toolbar
- **Size slider auto-syncs** to the selected object's thickness
- Hover highlight on any object in Select or Eraser mode
- Eraser now removes the **entire object** in one touch
- **Cursor changes** to the appropriate resize arrow when hovering over handles

### v1.0.1
- Fixed multiple-instance detection issue
- Improvements to the auto-update service

### v1.0.0
- Initial release

---

## License

MIT License — free to use, modify, and distribute.
