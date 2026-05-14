# Furniture Factory CRM

Информационная система для учёта деятельности мебельной фабрики: персонал, номенклатура, заказы клиентов, склад сырья, закупки материалов и отчёты. Проект состоит из **REST API на ASP.NET Core**, **настольного клиента (WPF)** и **веб‑клиента** (статический HTML/CSS/JS), использующих одно и то же API.

---

## Состав репозитория

| Каталог | Назначение |
|---------|------------|
| `backend/APIFurnitureCRM` | Веб‑API, Entity Framework Core, SQLite |
| `desktop-client/FurnitureCRMClient` | Клиент для Windows (WPF), .NET 8 |
| `web-client` | Браузерный клиент: вход и панели по ролям |

---

## Роли пользователей

В системе заложены как минимум роли **директор**, **менеджер**, **кладовщик**. Веб‑интерфейс перенаправляет пользователя на свою панель после входа по строке должности из ответа `POST /api/auth/login`.

---

## Требования к окружению

- **.NET SDK 8** (для API и десктопа)
- **Windows** — для сборки и запуска WPF‑клиента
- Для веб‑клиента достаточно современного браузера; удобно открывать страницы через локальный HTTP‑сервер (например, расширение Live Server в VS Code) или с `file://` при включённом CORS на API (в проекте политика `WebDev` разрешает любой origin)

---

## Backend (API)

- **Платформа:** ASP.NET Core 8, контроллеры REST, Swagger
- **БД:** SQLite, файл по умолчанию `furniture.db` рядом с рабочей директорией процесса API (см. `appsettings.json` → `ConnectionStrings:DefaultConnection`)
- **Порт по умолчанию:** `http://localhost:5028` (профиль `http` в `Properties/launchSettings.json`)

### Запуск

```bash
cd backend/APIFurnitureCRM
dotnet run
```

После старта откройте Swagger: `http://localhost:5028/swagger`.

### Основные группы эндпоинтов (префикс `/api`)

- `auth` — вход
- `staff` — сотрудники
- `clients` — клиенты
- `nomenclature` — номенклатура (в т.ч. снятые с производства)
- `orders` — заказы на продукцию
- `materials`, `materialorders` — сырьё и заказы закупки
- `reports` — отчёты (в т.ч. производство, аналитика по менеджерам)

Бизнес‑правила частично реализованы в БД (триггеры SQLite): статусы заказов, списание сырья, ограничения по ролям и т.д.

---

## Desktop-клиент (WPF)

- **Платформа:** .NET 8, WPF, CommunityToolkit.Mvvm, OxyPlot (графики), `HttpClient` к API
- **Запуск:** открыть `desktop-client/FurnitureCRMClient.sln` в Visual Studio / Rider, стартовый проект — `FurnitureCRMClient`, F5. В коде базовый URL API задан как `http://localhost:5028/api/` (см. окна и вкладки).

---

## Web-клиент

Статические файлы в каталоге `web-client`:

- `index.html` — вход (`js/common.js`, `js/login.js`)
- `director.html`, `manager.html`, `warehouse.html` — панели ролей
- `css/app.css` — оформление
- `js/common.js` — базовый URL API, сессия (`sessionStorage`), `apiJson`, проверки ролей, форматирование дат/сумм
- `js/director.js`, `js/manager.js`, `js/warehouse.js` — логика страниц
- `js/production-charts.js`, `js/material-charts.js`, `js/chart-theme.js` — диаграммы Chart.js
- `js/search-combobox.js` — поисковый комбобокс (например, в форме заказа менеджера)

### Подключение к API

По умолчанию в `js/common.js` задано:

```js
window.CRM_API_BASE = window.CRM_API_BASE || "http://localhost:5028/api";
```

Если API на другом хосте/порту, перед подключением `common.js` можно задать:

```html
<script>window.CRM_API_BASE = "http://127.0.0.1:ВАШ_ПОРТ/api";</script>
<script src="js/common.js"></script>
```

### Запуск

1. Запустите backend (`dotnet run` из `backend/APIFurnitureCRM`).
2. Откройте `web-client/index.html` через локальный сервер **или** напрямую из файловой системы — CORS в API настроен под разработку.

Графики подгружаются с CDN: `chart.js` (см. теги `<script>` в HTML панелей).

---

## Связь компонентов

```
[ Браузер: web-client ] ──HTTP JSON──► [ API :5028 ] ◄──HTTP JSON── [ WPF desktop ]
                                              │
                                         [ SQLite ]
```

Один экземпляр API и одна база; клиенты не хранят бизнес‑данные локально, только сессию пользователя (веб — в `sessionStorage`).
