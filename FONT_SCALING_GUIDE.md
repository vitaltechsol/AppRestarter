# Font Scaling Implementation Guide

## Overview
This application implements a centralized font scaling system that allows users to adjust the base font size (6.0 - 24.0) while maintaining proper UI layout and readability.

## How It Works

### 1. FontManager Class (`FontManager.cs`)
The `FontManager` static class handles all font-related operations:

- **BaseFontSize Property**: Global font size setting (default: 9.0, range: 6.0-24.0)
- **GetFont()**: Creates fonts with relative sizing (e.g., 1.0 = base, 1.1 = 10% larger)
- **ConfigureFormForScaling()**: One-time setup for forms after InitializeComponent()
- **ApplyScaledFontToForm()**: Updates fonts without layout changes
- **Excluded Controls**: Navigation buttons (btnNavApps, btnNavPcs, btnNavSettings) maintain fixed size

### 2. Settings Storage
Font size is stored in:
- `AppSettings.BaseFontSize` property
- XML configuration (`applications.xml` → `Settings/BaseFontSize`)
- Loaded on startup, saved when changed

### 3. Form Implementation Pattern

#### For Dialog Forms (SettingsForm, AddAppForm, GroupsForm)
```csharp
public MyForm()
{
	InitializeComponent();

	// Apply font scaling - call AFTER InitializeComponent()
	FontManager.ConfigureFormForScaling(this);

	// Rest of initialization...
}
```

#### For Main Form (Form1)
```csharp
public Form1()
{
	InitializeComponent();
	ApplyDarkTheme();
	LoadSettingsFromXml();

	// Initialize font system with loaded settings
	FontManager.BaseFontSize = _settings.BaseFontSize;
	// Don't call ConfigureFormForScaling for main form

	// Rest of initialization...
}
```

### 4. Dynamically Created Controls

#### Card Sizing
Cards (app cards, PC cards) scale proportionally with font size:

```csharp
// Calculate scale factor based on base font size (9.0)
float fontScale = FontManager.BaseFontSize / 9.0f;
int scaledWidth = (int)(200 * Math.Max(1.0f, fontScale));
int scaledHeight = (int)(42 * Math.Max(1.0f, fontScale));

panel.Width = scaledWidth;
panel.Height = scaledHeight;
```

#### Text Hierarchy in Cards
Maintain size hierarchy with smaller metadata text:

```csharp
// For App Cards
var nameFont = FontManager.GetFont(1.0f, FontStyle.Regular);  // Base size for app name
var metaFont = FontManager.GetFont(0.75f, FontStyle.Regular); // 25% smaller for PC/process

var lblAppName = new Label
{
	AutoSize = false, // Must be false to force single line with ellipsis
	Text = app.Name,
	Font = nameFont,  // Full size - prominent
	Location = new Point(6, 4), // Pin to top
	Size = new Size(cardWidth - 30, nameFont.Height + 2), // Fixed height based on font
	AutoEllipsis = true
};

var lblPcName = new Label
{
	AutoSize = true,
	Text = pcName,
	Font = metaFont,  // 25% smaller - subdued
	MaximumSize = new Size(cardWidth - 30, 0),
	AutoEllipsis = true
};
// Pin to bottom by calculating position after control creation
lblPcName.Location = new Point(6, cardPanel.Height - lblPcName.PreferredHeight - 4);

// For PC Cards
var pcNameFont = FontManager.GetFont(1.0f, FontStyle.Regular);  // Base size for PC name
var ipFont = FontManager.GetFont(0.75f, FontStyle.Regular);     // 25% smaller for IP

var lblPcName = new Label
{
	AutoSize = false, // Must be false to force single line with ellipsis
	Text = pc.Name,
	Font = pcNameFont,  // Full size - prominent
	Location = new Point(8, 8), // Pin to top
	Size = new Size(cardWidth - 16, pcNameFont.Height + 2), // Fixed height based on font
	AutoEllipsis = true
};

var lblIpAddress = new Label
{
	AutoSize = true,
	Text = pc.IP,
	Font = ipFont,  // 25% smaller - subdued
	MaximumSize = new Size(cardWidth - 16, 0),
	AutoEllipsis = true
};
// Pin to bottom by calculating position after control creation
lblIpAddress.Location = new Point(8, cardPanel.Height - lblIpAddress.PreferredHeight - 4);
```

#### Status Indicators
Status dots also scale with font:

```csharp
float dotScale = FontManager.BaseFontSize / 9.0f;
int dotSize = (int)(18 * Math.Max(1.0f, dotScale));

var statusLabel = new Label
{
	Text = "●",
	Font = FontManager.GetFont(1.0f, FontStyle.Bold),
	Size = new Size(dotSize, dotSize),
	TextAlign = ContentAlignment.MiddleCenter,
	Location = new Point(cardWidth - dotSize - 6, (cardHeight / 2) - (dotSize / 2))
};
```

### 5. Designer File Guidelines

#### Labels
```csharp
lblExample.AutoSize = true;
lblExample.Location = new Point(x, y);
// Do NOT set Size property
```

#### CheckBoxes
```csharp
chkExample.AutoSize = true;
chkExample.Location = new Point(x, y);
// Do NOT set Size property
```

#### Buttons (Non-Navigation)
```csharp
btnExample.Location = new Point(x, y);
btnExample.Size = new Size(width, height);  // OK to set fixed size
```

#### Form Properties
```csharp
// Standard dialog forms
FormBorderStyle = FormBorderStyle.FixedDialog;
// Do NOT add AutoScaleMode or AutoSize properties
```

### 6. Handling Font Changes

When font size changes in settings:

```csharp
if (Math.Abs(oldFontSize - _settings.BaseFontSize) > 0.01f)
{
	FontManager.BaseFontSize = _settings.BaseFontSize;

	// Refresh views with dynamically created controls
	if (_currentView == ViewMode.Apps)
		UpdateAppList();
	else
		RenderPcButtons();
}
```

## Best Practices

### ✅ DO
- Set `AutoSize = true` on standard Labels and CheckBoxes in designer
- Use `FontManager.GetFont()` for all dynamically created controls
- Use relative sizing for text hierarchy (1.0f for main text, 0.75f for metadata)
- Scale card dimensions proportionally with font size
- Use `AutoSize = false` with a fixed `Size` based on `Font.Height` for text that must remain on a single line
- Call `FontManager.ConfigureFormForScaling(this)` in form constructors
- Use `AutoEllipsis = true` on labels that might overflow

### ❌ DON'T
- Manually set font properties in designer
- Set fixed `Size` on labels/checkboxes in designer
- Use `new Font()` directly for UI controls
- Add `AutoScaleMode` or `AutoSize` to form properties in designer
- Apply fonts to navigation buttons (they're excluded)
- Use fixed card sizes - calculate based on font scale factor

## Excluded Controls

These controls maintain fixed sizing:
- `btnNavApps` - Circular navigation button
- `btnNavPcs` - Circular navigation button
- `btnNavSettings` - Circular navigation button

## Testing Checklist

When making UI changes, test with these font sizes:
- [ ] 6.0 (minimum)
- [ ] 9.0 (default)
- [ ] 12.0 (medium)
- [ ] 18.0 (large)
- [ ] 24.0 (maximum)

Verify:
- [ ] No text cutoff
- [ ] No horizontal scrollbars
- [ ] Buttons remain clickable
- [ ] Forms don't grow excessively
- [ ] Navigation buttons stay circular
- [ ] Dynamic controls (app/PC cards) scale properly

## Troubleshooting

### Text is cut off
- Ensure label has `AutoSize = true`
- Remove fixed `Size` property from designer
- Add `MaximumSize` with appropriate width

### Form is too large
- Check form doesn't have `AutoSize = true` in designer
- Verify only using `ConfigureFormForScaling()` for dialog forms

### Fonts not changing
- Ensure `FontManager.BaseFontSize` is set before creating controls
- For existing forms, call `FontManager.ApplyScaledFontToForm(this)`
- Refresh dynamic content (call UpdateAppList() or RenderPcButtons())

### Navigation buttons changed size
- Verify button names in `FontManager._excludedControlNames`
- Don't manually apply fonts to these buttons
