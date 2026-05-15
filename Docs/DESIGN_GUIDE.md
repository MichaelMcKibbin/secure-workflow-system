# Secure Workflow System - Design Guide

## Overview

This document outlines the design system and color palette used throughout the Secure Workflow System. It serves as a reference for developers and designers to maintain visual consistency and make informed decisions about styling and component usage.

---

## Color Palette

### Primary Brand Colors

Our color palette is inspired by nature and designed to evoke trust, stability, and professionalism.

#### Twilight Indigo (`#2b3a67`)
- **Primary Brand Color**
- **Usage**: Main headers, primary buttons, navigation, primary text
- **Semantic Role**: Primary actions and dominant UI elements
- **Variants**:
  - Dark: `#1f2847` (hover states)
  - Light: `#434d6f` (secondary emphasis)

```css
--color-twilight-indigo: #2b3a67;
--color-primary: var(--color-twilight-indigo);
--color-primary-dark: #1f2847;
--color-primary-light: #434d6f;
```

#### Blue Slate (`#496a81`)
- **Secondary Brand Color**
- **Usage**: Secondary buttons, links, secondary text, accents
- **Semantic Role**: Secondary actions and supporting UI elements
- **Variants**:
  - Dark: `#354a5f`
  - Light: `#5f7a92`

```css
--color-blue-slate: #496a81;
--color-secondary: var(--color-blue-slate);
--color-secondary-dark: #354a5f;
--color-secondary-light: #5f7a92;
```

#### Pacific Blue (`#66999b`)
- **Accent Color**
- **Usage**: Hover states, focus states, form interactions, table accents
- **Semantic Role**: Interactive feedback and accent highlights
- **Variants**:
  - Dark: `#4f7578`
  - Light: `#7aabae`

```css
--color-pacific-blue: #66999b;
--color-accent: var(--color-pacific-blue);
--color-accent-dark: #4f7578;
--color-accent-light: #7aabae;
```

#### Dry Sage (`#b3af8f`)
- **Neutral Color**
- **Usage**: Disabled states, muted text, neutral backgrounds
- **Semantic Role**: Neutral or de-emphasized UI elements
- **Variants**:
  - Dark: `#9a9776`
  - Light: `#c6c3a8`

```css
--color-dry-sage: #b3af8f;
--color-neutral: var(--color-dry-sage);
--color-neutral-dark: #9a9776;
--color-neutral-light: #c6c3a8;
```

#### Peach Glow (`#ffc482`)
- **Highlight Color**
- **Usage**: Call-to-action elements, important highlights, hover states in navigation
- **Semantic Role**: Warm highlights for important user attention
- **Variants**:
  - Dark: `#ffb758`
  - Light: `#ffd4a3`

```css
--color-peach-glow: #ffc482;
--color-highlight: var(--color-peach-glow);
--color-highlight-dark: #ffb758;
--color-highlight-light: #ffd4a3;
```

### Semantic Status Colors

Used for communicating status and actions to the user:

#### Success (`#6ba587`)
- **Usage**: Success messages, positive actions, approved states
- **Light variant**: `#9dd4b8`

#### Warning (`#e89b3c`)
- **Usage**: Warning messages, pending states, attention needed
- **Light variant**: `#f0b870`

#### Danger (`#c45b5b`)
- **Usage**: Error messages, destructive actions, closed states
- **Light variant**: `#d9888a`

#### Info (`#66999b`)
- **Usage**: Informational messages, new cases, open states
- **Light variant**: `#7aabae`

### Neutral Tones

#### Text Colors
- **Primary Text**: `var(--color-twilight-indigo)` - Main body text
- **Secondary Text**: `var(--color-blue-slate)` - Supporting text
- **Muted Text**: `#6c757d` - De-emphasized text

#### Backgrounds
- **Background**: `#fff` - Main background
- **Light Background**: `#f8f9fa` - Subtle background (cards, sections)
- **Lighter Background**: `#f5f5f5` - Emphasized light areas

#### Borders
- **Border**: `#ddd` - Standard borders
- **Dark Border**: `#bbb` - Emphasized borders

---

## Component Usage Guide

### Buttons

#### Primary Button (`.btn-primary`)
```html
<button class="btn btn-primary">Save</button>
```
- **Color**: Twilight Indigo
- **Usage**: Main actions, form submissions
- **Hover**: Darker indigo with shadow elevation

#### Secondary Button (`.btn-secondary`)
```html
<button class="btn btn-secondary">Cancel</button>
```
- **Color**: Blue Slate
- **Usage**: Alternative actions
- **Hover**: Darker slate with shadow elevation

#### Accent Button (`.btn-accent`)
```html
<button class="btn btn-accent">Create</button>
```
- **Color**: Pacific Blue
- **Usage**: Important secondary actions
- **Hover**: Darker pacific with shadow elevation

#### Outline Button (`.btn-outline-primary`)
```html
<button class="btn btn-outline-primary">Learn More</button>
```
- **Color**: Twilight Indigo (outline)
- **Usage**: Non-primary actions
- **Hover**: Filled with Twilight Indigo

### Hero Section (`.hero-section`)
```html
<div class="hero-section">
	<h1>Welcome</h1>
	<p>Descriptive text</p>
</div>
```
- **Background**: Linear gradient from Twilight Indigo to Blue Slate
- **Usage**: Page headers, welcome screens
- **Text Color**: White with Peach Glow highlights

### Cards (`.card`)
```html
<div class="card">
	<div class="card-header">
		<h5>Card Title</h5>
	</div>
	<div class="card-body">Content</div>
</div>
```
- **Border**: Left accent bar in Pacific Blue
- **Header**: Gradient background (Primary Light to Secondary Light)
- **Hover**: Slight lift effect with enhanced shadow
- **Elevation**: Subtle shadow for depth

### Case Cards (`.case-card`)
```html
<div class="case-card status-open">
	<h5>Case Title</h5>
	<p class="case-meta">Case details</p>
</div>
```
- **Variants**: `.status-open`, `.status-closed`, `.status-pending`
- **Border**: Colored left bar based on status
- **Background**: Subtle gradient based on status
- **Usage**: Case listing pages, dashboards

### Badges

#### Badge Styles
```html
<span class="badge badge-success">Active</span>
<span class="badge badge-warning">Pending</span>
<span class="badge badge-danger">Closed</span>
<span class="badge badge-info">New</span>
```

- **Success Badge**: Green background, white text
- **Warning Badge**: Orange background, white text
- **Danger Badge**: Red background, white text
- **Info Badge**: Pacific Blue background, white text
- **Usage**: Status indicators, user roles, email confirmation states

### Badges - Inline Status
- Use badges within table cells and cards to indicate state
- Combine with appropriate icon/emoji for better UX
- Example: `<span class="badge badge-info">ℹ️ New</span>`

### Tables (`.table`)
```html
<table class="table table-striped">
	<thead>
		<tr><th>Header</th></tr>
	</thead>
	<tbody>
		<tr><td>Data</td></tr>
	</tbody>
</table>
```
- **Header**: Gradient background (Primary Light to Secondary Light) with white text
- **Rows**: Alternate light backgrounds (on odd rows)
- **Hover**: Subtle background change with left accent bar
- **Borders**: Pacific Blue borders on header

### Forms

#### Form Labels (`.form-label`)
```html
<label class="form-label">Email Address</label>
```
- **Color**: Twilight Indigo
- **Font Weight**: 500 (medium)

#### Form Controls (`.form-control`, `.form-select`)
```html
<input class="form-control" type="email" />
<select class="form-select"></select>
```
- **Border**: Standard border color
- **Focus State**: Pacific Blue border with subtle shadow
- **Shadow**: Inset rounded effect on focus

### Alerts

#### Alert Styles
```html
<div class="alert alert-success">Success message</div>
<div class="alert alert-warning">Warning message</div>
<div class="alert alert-danger">Error message</div>
<div class="alert alert-info">Info message</div>
```

- **Border**: Left accent bar (3-4px)
- **Background**: Light tinted background
- **Text Color**: Status color (darker than background)
- **Usage**: System messages, validation feedback

### Section Headers (`.section-header`)
```html
<div class="section-header">
	<h2>Section Title</h2>
	<p class="section-subheader">Subtitle or description</p>
</div>
```
- **Border**: 3px bottom border in Pacific Blue
- **Title**: Twilight Indigo, bold
- **Subtitle**: Blue Slate, smaller font
- **Usage**: Page sections, major content areas

---

## Typography

### Headings
- **h1, h2, h3, h4, h5, h6**: Color `var(--color-primary)` (Twilight Indigo)
- **Font Family**: Calibri, Helvetica, Arial, sans-serif
- **Font Weight**: 600-700 for headings

### Body Text
- **Color**: `var(--color-text-primary)` (Twilight Indigo)
- **Font Size**: 1rem
- **Line Height**: 1.5

### Links
```html
<a href="#">Link Text</a>
```
- **Color**: Blue Slate
- **Hover**: Pacific Blue with underline
- **Decoration**: None (underline on hover)

---

## Spacing & Layout

### Padding/Margin Scale
- **Small (xs)**: 0.5rem (8px)
- **Medium (sm)**: 1rem (16px)
- **Large (md)**: 1.5rem (24px)
- **Extra Large (lg)**: 2rem (32px)

### Responsive Grid
- **Mobile**: Single column (100%)
- **Tablet**: 2 columns (md breakpoint, 768px)
- **Desktop**: 3+ columns (lg breakpoint, 1024px)

---

## Animations & Transitions

### Fade In (`.fade-in`)
- Duration: 0.3s
- Effect: Opacity and slight upward movement
- Usage: Card entries, element appearance

### Slide In (`.slide-in`)
- Duration: 0.3s
- Effect: Opacity and left-to-right movement
- Usage: Sidebar items, sequential element loading

### Hover Effects
- Buttons: 0.2s transition with transform (translate -1px)
- Cards: 0.3s transition with transform (translate -2px) and shadow
- Links: 0.2s transition with color change

---

## Best Practices

### Color Usage
1. **Primary Color** (Twilight Indigo): Use for main headers, primary buttons, and dominant UI
2. **Secondary Color** (Blue Slate): Use for supporting elements and secondary buttons
3. **Accent Color** (Pacific Blue): Use for interactive elements, hover states, and focus states
4. **Status Colors**: Always use appropriate semantic colors (success, warning, danger, info)
5. **Neutral**: Use for disabled, muted, or de-emphasized content

### Component Selection
1. **Primary Actions**: Use `.btn-primary` for main user workflows
2. **Secondary Actions**: Use `.btn-secondary` or `.btn-outline-primary` for alternatives
3. **List Views**: Use tables for structured data, cards for flexible layouts
4. **Status Display**: Use badges within tables/cards for quick status recognition
5. **Empty States**: Use `.alert alert-info` for "no data" messages

### Accessibility
1. Ensure sufficient color contrast (WCAG AA minimum)
2. Don't rely on color alone for status indication—use icons/text
3. Use semantic HTML (buttons, not divs styled as buttons)
4. Include focus states for keyboard navigation
5. Use descriptive link text, not "click here"

### Mobile Responsiveness
1. Use Bootstrap's grid system (`.row`, `.col-*`)
2. Stack cards vertically on mobile
3. Use responsive button sizes
4. Test on multiple screen sizes

---

## Examples

### Dashboard Card
```html
<div class="card fade-in">
	<div class="card-header">
		<h5 class="card-title">Dashboard</h5>
	</div>
	<div class="card-body">
		<p>Welcome content</p>
	</div>
</div>
```

### Status Table Row
```html
<tr>
	<td>Case #123</td>
	<td><span class="badge badge-info">Open</span></td>
	<td>Created: 2024-01-15</td>
</tr>
```

### Action Buttons
```html
<div class="button-group">
	<button class="btn btn-primary">Save</button>
	<button class="btn btn-secondary">Cancel</button>
	<button class="btn btn-outline-primary">Learn More</button>
</div>
```

---

## Related Files

- **Color Definitions**: `/wwwroot/app.css` (CSS variables)
- **Component Styles**: `/wwwroot/styles.css` (enhanced component styles)
- **Bootstrap Theme**: `/wwwroot/lib/bootstrap/` (Bootstrap library)

---

## Version History

- **v1.0** - Initial design system documentation with color palette, component guide, and best practices

---

## Contact & Questions

For design questions or suggestions, please open an issue in the GitHub repository or contact the team lead.
