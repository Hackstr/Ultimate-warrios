# LOBBY.GG — Design System

Единая дизайн-система для всех продуктов LOBBY.GG (основная платформа, админка компьютерных клубов и т.д.).

---

## Бренд

- Акцентный цвет: **Acid Lime** `#CDFE00`
- Эстетика: тёмный, киберспортивный, чистый, минималистичный
- Дефолтная тема: **Dark**
- Поддержка тем: Dark + Light через CSS-переменные и атрибут `data-theme` на `<html>`

---

## Цвета

### Dark Theme (дефолт)

| Токен | Переменная | Значение | Назначение |
|-------|-----------|----------|------------|
| Accent | `--lime` | `#CDFE00` | Акцент, кнопки, подсветка |
| Accent Dim | `--lime-dim` | `#9BBF00` | Приглушённый акцент |
| Accent Glow | `--lime-glow` | `rgba(205,254,0,0.15)` | Свечение вокруг элементов |
| Accent Glow Strong | `--lime-glow-strong` | `rgba(205,254,0,0.3)` | Сильное свечение (CTA) |
| BG Primary | `--bg-primary` | `#0A0A0A` | Фон страницы |
| BG Secondary | `--bg-secondary` | `#111111` | Фон секций |
| BG Card | `--bg-card` | `#161616` | Фон карточек |
| BG Card Hover | `--bg-card-hover` | `#1C1C1C` | Ховер карточек |
| BG Nav | `--bg-nav` | `rgba(10,10,10,0.8)` | Навбар (с blur) |
| Border | `--border-color` | `#222222` | Границы, разделители |
| Border Hover | `--border-hover` | `#333333` | Границы при ховере |
| Text Primary | `--text-primary` | `#FFFFFF` | Заголовки, основной текст |
| Text Secondary | `--text-secondary` | `#888888` | Описания |
| Text Muted | `--text-muted` | `#555555` | Подписи, лейблы |

### Light Theme

| Токен | Переменная | Значение |
|-------|-----------|----------|
| Accent | `--lime` | `#A8D600` |
| BG Primary | `--bg-primary` | `#FAFAFA` |
| BG Secondary | `--bg-secondary` | `#F0F0F0` |
| BG Card | `--bg-card` | `#FFFFFF` |
| BG Card Hover | `--bg-card-hover` | `#F5F5F5` |
| BG Nav | `--bg-nav` | `rgba(250,250,250,0.85)` |
| Border | `--border-color` | `#E0E0E0` |
| Border Hover | `--border-hover` | `#CCCCCC` |
| Text Primary | `--text-primary` | `#0A0A0A` |
| Text Secondary | `--text-secondary` | `#666666` |
| Text Muted | `--text-muted` | `#999999` |

**Правила Light-темы:**
- Кнопки: **чёрный фон `#0A0A0A` + белый текст** — НЕ lime
- Бейджи/лейблы: **чёрный фон + белый текст** — НЕ lime
- Hover-состояния: чёрные бордеры — НЕ lime
- Акцентный текст: lime-градиент `linear-gradient(135deg, #CDFE00, #A8D600)` с `background-clip: text`
- Никакого оливкового/мутного зелёного (#6B8C00) — выглядит грязно на светлом

### Семантические цвета (обе темы)

| Цвет | Значение | Назначение |
|------|----------|------------|
| Destructive / Error | `#EF4444` | Ошибки, удаление |
| Success | `#10B981` | Успех, позитивные состояния |
| Info | `#3B82F6` | Информация |
| Warning | `#F59E0B` | Предупреждения |

### Цветовая кодировка данных

- Хорошо (win rate > 55%, K/D > 1.2): `#10B981` (зелёный)
- Средне: `#F59E0B` (жёлтый)
- Плохо (win rate < 45%, K/D < 0.8): `#EF4444` (красный)

---

## Типографика

### Шрифты

| Шрифт | CSS-класс | Назначение | Веса |
|-------|-----------|------------|------|
| **Space Grotesk** | `font-heading` | Заголовки, кнопки, жирный UI-текст | 400, 500, 600, 700 |
| **JetBrains Mono** | `font-mono` / `font-sans` (дефолт body) | Основной текст, данные, моноширинные элементы | 300–800 |

Подключение:
```
https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@300;400;500;600;700;800&family=Space+Grotesk:wght@400;500;600;700&display=swap
```

### Масштаб

| Элемент | Размер | Вес | Доп. |
|---------|--------|-----|------|
| H1 | `3rem` (48px) | 700 | `letter-spacing: -0.02em` |
| H2 | `2.25rem` (36px) | 600 | |
| H3 | `1.75rem` (28px) | 600 | |
| H4–H6 | по убыванию | 600 | `letter-spacing: -0.01em` |
| Body | `1rem` (16px) | 400 | `line-height: 1.7` |
| Small / Labels | `0.75rem` (12px) | 500 | `text-transform: uppercase; letter-spacing: 0.08em` |

---

## Пространство и радиусы

### Border Radius

| Элемент | Значение | Tailwind |
|---------|----------|----------|
| Карточки | 16px | `rounded-2xl` |
| Кнопки | pill / 9999px | `rounded-full` |
| Инпуты | 12px | `rounded-xl` |
| Бейджи | pill | `rounded-full` |
| Иконки-контейнеры | 12px | `rounded-xl` |
| Базовый `--radius` | 12px (0.75rem) | — |

### Отступы

| Элемент | Padding |
|---------|---------|
| Карточки | `p-7` (28px) |
| Кнопки (default) | `px-6 py-2.5`, высота `h-11` |
| Кнопки (lg) | `px-8`, высота `h-12` |
| Кнопки (sm) | `px-4`, высота `h-9` |
| Инпуты | `px-4 py-2.5`, высота `h-11` |
| Страница | `px-10` (40px горизонтально) |

---

## Тени и эффекты

- **Dark theme**: минимум теней, акцент на бордерах + glow-эффекты
  - CTA glow: `box-shadow: 0 0 40px rgba(205, 254, 0, 0.3)`
  - Иконки: обёртка с `background: var(--lime-glow)` (rgba 15%)
- **Light theme**: `shadow-sm` дефолт, `shadow-lg` на ховере
- **Навбар**: `backdrop-filter: blur(12px)` + полупрозрачный фон

---

## Компоненты

### Button

Варианты:
| Вариант | Стиль |
|---------|-------|
| `default` | Lime фон `var(--lime)`, чёрный текст `#0A0A0A`. Hover: `#b8e500` |
| `destructive` | Красный фон `#EF4444`, белый текст |
| `outline` | Прозрачный, бордер `var(--border-color)`, текст `var(--text-primary)` |
| `secondary` | Фон `var(--bg-secondary)`, текст `var(--text-primary)` |
| `ghost` | Прозрачный, текст `var(--text-primary)`. Hover: фон `var(--bg-secondary)` |
| `link` | Текст `var(--lime)`, подчёркивание при ховере |

Размеры: `default` (h-11), `sm` (h-9), `lg` (h-12), `icon` (h-11 w-11)

Форма: `rounded-full` (pill). Шрифт: `font-heading` (Space Grotesk).

### Card

- Фон: `var(--bg-card)`, бордер: `var(--border-color)`, радиус: `rounded-2xl`
- Hover: `shadow-lg`, transition 300ms
- Паддинг: `p-7`
- Заголовок секции внутри карточки: uppercase, 11px, `letter-spacing: 0.08em`, с иконкой

### Badge

- Форма: `rounded-full`, padding `px-3 py-1`, размер текста `text-xs`
- `default`: lime фон, чёрный текст
- `secondary`: `var(--bg-secondary)` фон
- `outline`: бордер `var(--border-color)`
- `destructive`: красный фон, белый текст

### Input

- Высота: `h-11`, радиус: `rounded-xl`, бордер: `2px solid var(--border-color)`
- Фокус: бордер `#CDFE00` + ring `rgba(205,254,0,0.2)`
- Плейсхолдер: `var(--text-muted)`

### Skeleton (Loading)

- `animate-pulse`, `rounded-md`, фон `primary/10`

---

## Иконки

- Библиотека: **lucide-react** — единственная разрешённая
- Размеры: `w-4 h-4` (в кнопках), `w-5 h-5` (обычные), `w-6 h-6` (feature cards)
- Цвет: через `style={{ color: 'var(--lime)' }}` или `var(--text-secondary)`
- Контейнер: `w-12 h-12 rounded-xl bg-[var(--lime-glow)] flex items-center justify-center`
- **Нельзя**: emoji в UI, другие icon-библиотеки

---

## Тема — реализация

```html
<!-- В index.html ДО React — предотвращает flash -->
<script>
  (function(){
    var t = localStorage.getItem('lobby-theme');
    document.documentElement.setAttribute('data-theme', t === 'light' ? 'light' : 'dark');
  })();
</script>
```

- Хранение: `localStorage` ключ `lobby-theme` (`'dark'` | `'light'`)
- Переключение: `document.documentElement.setAttribute('data-theme', theme)`
- Все элементы: `transition: background 0.3s, color 0.3s, border-color 0.3s`
- CSS-переменные меняются автоматически через `[data-theme="light"]` / `:root`

---

## Стилизация — правила

1. **Цвета только через CSS-переменные** — никогда не хардкодить hex (кроме семантических: `#EF4444`, `#10B981` и т.д.)
2. **Ховер с CSS-переменными** — Tailwind не поддерживает `hover:bg-[var(...)]`, поэтому используем `onMouseEnter`/`onMouseLeave` + inline styles
3. **Tailwind** — для layout, spacing, типографики, flex/grid
4. **CSS-переменные через `style={{}}`** — для цветов, фонов, бордеров
5. **Утилита `cn()`** — `clsx` + `tailwind-merge` для мержа классов
6. **Варианты через CVA** (class-variance-authority) — для компонентов с множественными стилями
7. **framer-motion** — для анимаций и переходов

---

## Layout-принципы

1. **Full-width контент** — никаких произвольных `max-width`, контент заполняет всю ширину с паддингом `px-10`
2. **Data density** — показывать значимые данные, скрывать нулевые/очевидные
3. **Визуальная иерархия**: Hero → Stats Grid → Tab Navigation → Content Cards
4. **Карточки**: все секции в `.card` паттерне — `bg-card`, `border`, `rounded-2xl`, `p-5`
5. **Списки внутри карточек**: `bg-secondary` строки, `rounded-xl`, gap 6px

---

## Стек

| Слой | Технология |
|------|-----------|
| Framework | React 18 + TypeScript |
| Build | Vite |
| Styling | Tailwind CSS + CSS Variables |
| UI Primitives | Radix UI + shadcn/ui паттерн |
| Variants | class-variance-authority (CVA) |
| Class Merge | clsx + tailwind-merge (`cn()`) |
| Animations | framer-motion |
| Icons | lucide-react |
| Fonts | Google Fonts (Space Grotesk + JetBrains Mono) |
