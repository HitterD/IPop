# IPop Phase 4 — IM Mail-Style + Desktop Popup: UX Design Spec

**Date:** 2026-05-13
**Phase:** 4 — IM + Desktop Popup
**Status:** Approved by user
**Design direction:** Option A — Three-Pane Classic + Thread Timeline (collapsible)

---

## 1. Design System Alignment

Phase 4 IM uses the **exact same design tokens** as the existing chat module (`chat.css`):

| Token | Value |
|---|---|
| `--ipop-accent` | `#2DAA9E` |
| `--ipop-accent-strong` | `#1f746b` |
| `--ipop-accent-soft` | `#E6F4F2` |
| `--ipop-ink` | `#102A2A` |
| `--ipop-ink-soft` | `#5F7474` |
| `--ipop-surface` | `#FBFBF7` |
| `--ipop-surface-raised` | `#FFFFFF` |
| `--ipop-line` | `rgba(16,42,42,0.08)` |
| `--ipop-shadow` | `rgba(16,42,42,0.25)` |

**Typography:** Plus Jakarta Sans (headings, labels) + Inter (body). Same as chat.
**Border radius:** 12–20px. Same as chat.
**Animations:** Same cubic-bezier `(.2,.8,.2,1)` spring easing, 160–520ms range.

---

## 2. Layout — Three-Pane Shell

```
┌──────────────────────────────────────────────────────────────────┐
│  .ipop-im-shell  (grid: 190px | 300px | 1fr)                    │
│  border-radius: 20px, same shadow as .ipop-chat-shell            │
├──────────────┬──────────────────────┬───────────────────────────┤
│  SIDEBAR     │  MESSAGE LIST        │  PREVIEW PANE             │
│  .im-sidebar │  .im-list            │  .im-preview              │
│              │                      │                           │
│  IPop IM     │  [search bar]        │  Subject (h1)             │
│  [Compose]   │  ─────────────────   │  From · To · Date · chips │
│  Inbox  14   │  ● AW  Budget Q2     │  [Reply][ReplyAll][Fwd]   │
│  Sent   22   │    09:41             │  ─────────────────────    │
│  Deleted     │  ● HR  Policy…       │  THREAD TIMELINE          │
│  ──────────  │    08:10             │  (collapsible nodes)      │
│  Filters     │  ○ IT  VPN…          │  ─────────────────────    │
│  Unread      │    07:55             │  [Quick reply textarea]   │
│  Attachments │  ○ FN  Recon…        │  [Send ↑]                 │
│  ──────────  │    07:12             │                           │
│  Labels      │                      │                           │
│  ● Finance   │                      │                           │
│  ● HR        │                      │                           │
│  ● IT        │                      │                           │
└──────────────┴──────────────────────┴───────────────────────────┘
```

---

## 3. Sidebar

- **Compose button:** teal filled, Plus Jakarta Sans 700, box-shadow `0 10px 24px -14px var(--ipop-accent)`, hover lift `-1px`.
- **Folder items:** `Inbox`, `Sent`, `Deleted`. Active state: `--ipop-accent-soft` bg + teal border.
- **Unread badge:** teal pill, white text. Ghost badge (Sent): surface bg, ink-soft text.
- **Dividers + label sections:** `Filters` (Unread, Attachments) and `Labels` (color dot + name).
- **Animation:** sidebar fades in with `opacity 0→1, translateY 12px→0` on page load (same as `.ipop-chat-sidebar`).

---

## 4. Message List

- **Search bar:** pill shape, surface bg, ink-soft placeholder. Same as `.ipop-chat-search`.
- **Message row grid:** `8px (unread dot) | 30px (avatar) | 1fr (meta) | auto (time)`.
- **Unread dot:** teal circle with soft glow ring `box-shadow: 0 0 0 3px color-mix(in srgb, var(--ipop-accent) 18%, transparent)`.
- **Avatar:** 30px circle, teal gradient, initials. Same as `.ipop-chat-person__avatar`.
- **Active row:** `--ipop-accent-soft` bg + teal border + `translateX(2px)` shift.
- **Stagger animation:** rows enter with `rowIn` keyframe (opacity 0→1, translateY 6px→0), delay 0/60/120/180/240ms.
- **Hover:** `--ipop-accent-soft` bg, 160ms ease.

---

## 5. Preview Pane — Header

- **Subject:** Plus Jakarta Sans 800, 17px, `letter-spacing: -0.01em`.
- **Meta row:** From (bold), To (bold), date, `chip.urg` (amber `#FEF3C7`/`#92400E`), `chip` (teal soft).
- **Reply count:** right-aligned, ink-soft, "3 replies · 4 people".
- **Action buttons:** `[Reply]` (teal primary), `[Reply All]`, `[Forward]`, `[Delete]` (right-aligned). Same `.btn` style as chat composer actions.

---

## 6. Thread Timeline (collapsible)

Each reply in a thread is a **collapsible node** on a vertical timeline:

```
  ●──────────────────────────────────────
  │  [AW] Andi Wijaya  Selasa 13 Mei · 09:41  ▾
  │  Body text of original message.
  │  [PDF] Budget-Q2-Draft.pdf  [XLS] Allocation.xlsx
  │
  ●──────────────────────────────────────  ← "me" node (teal dot)
  │  [YO] You  09:48  ▾  (collapsed by default)
  │
  ●──────────────────────────────────────
  │  [AW] Andi Wijaya  09:52  ▾  (collapsed)
  │
  ●──────────────────────────────────────
     [BS] Budi Santoso  10:05  ▾  (collapsed, last node — no line)
```

**Timeline line:** `width: 2px`, `color-mix(in srgb, var(--ipop-accent) 20%, transparent)`, absolute positioned left.

**Node dot:**
- Others: `--ipop-accent-soft` bg + teal border.
- Me: solid teal bg + teal-strong border.
- Hover: `scale(1.2)` + glow ring.

**Thread card:**
- Default: white bg, `--ipop-line` border, `border-radius: 14px`.
- Me card: gradient `var(--r) → var(--af)` bg + teal border.
- Hover: `box-shadow: 0 8px 24px -12px var(--ipop-shadow)`.

**Collapse toggle:**
- Click header row to expand/collapse.
- Arrow `▾` rotates `-90deg` when collapsed (CSS transition 240ms).
- First message (original) expanded by default; replies collapsed.

**Quoted block:** left border `3px solid var(--ipop-accent)`, surface bg, ink-soft text.

**Attachments:** pill chips with file type label (bold teal) + filename + size. Hover: `--ipop-accent-soft` bg.

---

## 7. Quick Reply

- Textarea: `border-radius: 12px`, focus ring `0 0 0 4px color-mix(in srgb, var(--ipop-accent) 14%, transparent)`.
- Send button: teal pill, `box-shadow: 0 8px 20px -12px var(--ipop-accent)`, hover lift.
- Hint row: "Shift+Enter newline · 📎 Attach · Full compose →".
- "Full compose →" opens full compose modal/page.

---

## 8. Desktop Popup Notification

Legacy-compatible `SignalRPayload { Id, ApplicationName, Message, Url }`.

**Visual:**
- Fixed bottom-right, `width: 320px`, `border-radius: 16px`, white bg, soft shadow.
- `IPop IM` app tag: teal-soft bg, teal-strong text, uppercase 8px.
- Sender avatar (30px teal gradient circle) + bold title + preview snippet.
- `[Open]` (teal primary) + `[Dismiss]` buttons.
- `×` close button top-right.

**Animation:** `popIn` keyframe — `opacity 0→1, translateY(20px)→0, scale(0.96)→1`, 520ms spring easing.

**Behavior:**
- Auto-dismiss after 8 seconds.
- Click Open → navigate to `/im?messageId={id}` and mark as read.
- Click Dismiss or × → remove without marking read.

---

## 9. Compose Modal

Full compose opens as a modal overlay (not a separate page):
- Recipient chips (same style as chat attachment pills).
- Subject input.
- Rich text body (reuse existing `IChatContentSanitizer` pattern).
- Attachment upload (reuse `/api/chat/attachments` endpoint pattern).
- `[Send]` primary + `[Save Draft]` secondary + `[Cancel]`.
- PRG + idempotency key on submit.

---

## 10. Responsive

- `≤ 860px`: stack to single column, same breakpoint as chat.
- List becomes full-width; preview slides in as overlay.

---

## 11. Accessibility

- All interactive elements keyboard-navigable.
- Thread toggle buttons have `aria-expanded` attribute.
- Unread dot has `aria-label="unread"`.
- Color contrast: teal on white passes WCAG AA (4.5:1+).
- `prefers-reduced-motion`: disable all animations (same media query as chat.css).

---

## 12. CSS Strategy

- Add `im.css` to `src/IPop.Host/wwwroot/css/` — import same tokens from `chat.css` (or share via `:root` already in chat.css).
- Class prefix: `.ipop-im-*` to avoid collision with `.ipop-chat-*`.
- Reuse: `.ipop-chat-person__avatar`, `.ipop-ext-chip`, `.ipop-chat-search`, `.ipop-chat-composer textarea`, `.ipop-chat-bubble` animation keyframes.

---

## 13. Mockup Reference

Interactive mockup: `d:/IPop Project/IPop/.superpowers/brainstorm/1196-1778661199/content/option-a-thread.html`