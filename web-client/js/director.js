(function () {
  const user = requireDirectorPage();
  if (!user) return;

  document.getElementById("user-line").textContent =
    `${user.fullName} · ${user.position}`;

  const panels = {
    staff: document.getElementById("panel-staff"),
    nom: document.getElementById("panel-nom"),
    prod: document.getElementById("panel-prod"),
    sales: document.getElementById("panel-sales"),
  };

  function activateTab(key) {
    document.querySelectorAll(".tab").forEach((btn) => {
      const k = btn.getAttribute("data-panel");
      const on = k === key;
      btn.classList.toggle("active", on);
      btn.setAttribute("aria-selected", on ? "true" : "false");
    });
    Object.entries(panels).forEach(([k, el]) => {
      el.classList.toggle("active", k === key);
    });
  }

  document.getElementById("tabs").addEventListener("click", (e) => {
    const btn = e.target.closest(".tab");
    if (!btn) return;
    activateTab(btn.getAttribute("data-panel"));
  });

  document.getElementById("btn-logout").addEventListener("click", () => {
    clearStoredUser();
    window.location.href = "index.html";
  });

  function tbodyClear(tb) {
    while (tb.firstChild) tb.removeChild(tb.firstChild);
  }

  function trCells(tb, cells) {
    const tr = document.createElement("tr");
    cells.forEach((text) => {
      const td = document.createElement("td");
      td.textContent = text == null ? "—" : String(text);
      tr.appendChild(td);
    });
    tb.appendChild(tr);
  }

  /* ---------- Сотрудники ---------- */

  /** @type {any[]} */
  let staffList = [];

  const stBody = document.getElementById("st-body");
  const stMode = document.getElementById("st-mode");
  const stPosition = document.getElementById("st-position");
  const stSearch = document.getElementById("st-search");

  function rebuildPositionFilter() {
    const prev = stPosition.value;
    const set = new Set();
    staffList.forEach((s) => {
      if (s.position) set.add(s.position);
    });
    const list = Array.from(set).sort((a, b) => a.localeCompare(b, "ru"));
    stPosition.innerHTML = "";
    const all = document.createElement("option");
    all.value = "";
    all.textContent = "Все должности";
    stPosition.appendChild(all);
    list.forEach((p) => {
      const opt = document.createElement("option");
      opt.value = p;
      opt.textContent = p;
      stPosition.appendChild(opt);
    });
    if (prev && [...stPosition.options].some((o) => o.value === prev)) {
      stPosition.value = prev;
    }
  }

  function staffFiltered() {
    const pos = stPosition.value;
    const q = stSearch.value.trim().toLowerCase();
    return staffList.filter((s) => {
      if (pos && s.position !== pos) return false;
      const name = (s.fullName || "").toLowerCase();
      if (q && !name.includes(q)) return false;
      return true;
    });
  }

  function accountLabel(s) {
    const ua = s.userAccount;
    if (!ua) return "Нет записи";
    return ua.isActive ? "Активна" : "Заблокирована";
  }

  function renderStaff() {
    tbodyClear(stBody);
    staffFiltered().forEach((s) => {
      const ua = s.userAccount;
      trCells(stBody, [
        s.staffId,
        s.fullName,
        s.position,
        ua && ua.username ? ua.username : "—",
        accountLabel(s),
      ]);
    });
  }

  async function loadStaff() {
    const endpoint =
      stMode.value === "fired" ? "staff/fired" : "staff";
    staffList = await apiJson(endpoint, { method: "GET" });
    rebuildPositionFilter();
    renderStaff();
  }

  stMode.addEventListener("change", loadStaff);
  stPosition.addEventListener("change", renderStaff);
  stSearch.addEventListener("input", renderStaff);
  document.getElementById("st-refresh").addEventListener("click", loadStaff);

  /* ---------- Номенклатура ---------- */

  /** @type {any[]} */
  let nomenclature = [];

  const nomBody = document.getElementById("nom-body");
  const nomMode = document.getElementById("nom-mode");
  const nomSearch = document.getElementById("nom-search");
  const nomCategory = document.getElementById("nom-category");
  const nomMin = document.getElementById("nom-min");
  const nomMax = document.getElementById("nom-max");

  function nomRebuildCategories() {
    const prev = nomCategory.value;
    const set = new Set();
    nomenclature.forEach((p) => {
      if (p.category) set.add(p.category);
    });
    const list = Array.from(set).sort((a, b) => a.localeCompare(b, "ru"));
    nomCategory.innerHTML = "";
    const all = document.createElement("option");
    all.value = "";
    all.textContent = "Все категории";
    nomCategory.appendChild(all);
    list.forEach((c) => {
      const opt = document.createElement("option");
      opt.value = c;
      opt.textContent = c;
      nomCategory.appendChild(opt);
    });
    if (prev && [...nomCategory.options].some((o) => o.value === prev)) {
      nomCategory.value = prev;
    }
  }

  function nomFiltered() {
    const q = nomSearch.value.trim().toLowerCase();
    const cat = nomCategory.value;
    const min = nomMin.value ? Number(nomMin.value) : null;
    const max = nomMax.value ? Number(nomMax.value) : null;
    return nomenclature.filter((p) => {
      const name = (p.name || "").toLowerCase();
      if (q && !name.includes(q)) return false;
      if (cat && p.category !== cat) return false;
      if (min != null && !Number.isNaN(min) && p.price < min) return false;
      if (max != null && !Number.isNaN(max) && p.price > max) return false;
      return true;
    });
  }

  function renderNom() {
    tbodyClear(nomBody);
    nomFiltered().forEach((p) => {
      trCells(nomBody, [
        p.productId,
        p.name,
        p.category,
        p.color,
        p.dimensions,
        formatMoney(p.price),
      ]);
    });
  }

  async function loadNom() {
    const path =
      nomMode.value === "notproduced"
        ? "nomenclature/notproduced"
        : "nomenclature";
    nomenclature = await apiJson(path, { method: "GET" });
    nomRebuildCategories();
    renderNom();
  }

  nomMode.addEventListener("change", loadNom);
  ["input", "change"].forEach((ev) => {
    nomSearch.addEventListener(ev, renderNom);
    nomCategory.addEventListener(ev, renderNom);
    nomMin.addEventListener(ev, renderNom);
    nomMax.addEventListener(ev, renderNom);
  });
  document.getElementById("nom-clear").addEventListener("click", () => {
    nomSearch.value = "";
    nomMin.value = "";
    nomMax.value = "";
    nomCategory.value = "";
    renderNom();
  });
  document.getElementById("nom-refresh").addEventListener("click", loadNom);

  /* ---------- Отчёт о производстве ---------- */

  const prodStats = document.getElementById("prod-stats");
  const prodBody = document.getElementById("prod-body");
  const prodMsg = document.getElementById("prod-msg");

  document.getElementById("prod-run").addEventListener("click", async () => {
    prodMsg.textContent = "";
    tbodyClear(prodBody);
    prodStats.style.display = "none";
    prodStats.innerHTML = "";
    if (typeof destroyProductionReportCharts === "function") {
      destroyProductionReportCharts();
    }
    const dirCharts = document.getElementById("dir-prod-charts-wrap");
    if (dirCharts) dirCharts.style.display = "none";

    const from = document.getElementById("prod-from").value;
    const to = document.getElementById("prod-to").value;
    const qs = [];
    if (from) qs.push(`from=${encodeURIComponent(from)}`);
    if (to) qs.push(`to=${encodeURIComponent(to)}`);
    const path =
      qs.length > 0
        ? `reports/production?${qs.join("&")}`
        : "reports/production";

    try {
      const data = await apiJson(path, { method: "GET" });
      const stats = [
        ["Всего заказов", data.totalOrders],
        ["В обработке", data.inProcessingCount],
        ["В производстве", data.inProductionCount],
        ["Выполнено", data.completedCount],
        ["Отменено", data.cancelledCount],
        ["Выручка (выполненные)", formatMoney(data.totalRevenue)],
      ];
      stats.forEach(([k, v]) => {
        const box = document.createElement("div");
        box.className = "stat";
        box.innerHTML = `<div class="k"></div><div class="v"></div>`;
        box.querySelector(".k").textContent = k;
        box.querySelector(".v").textContent = String(v);
        prodStats.appendChild(box);
      });
      prodStats.style.display = "grid";

      (data.orders || []).forEach((o) => {
        trCells(prodBody, [
          o.orderId,
          formatRuDate(o.orderDate),
          o.productName,
          o.quantity,
          o.status,
          formatMoney(o.totalPrice),
        ]);
      });

      if (typeof renderProductionReportCharts === "function") {
        renderProductionReportCharts("dir-prod-charts-wrap", data.orders || []);
      }
    } catch (e) {
      prodMsg.className = "msg error";
      prodMsg.textContent = e.message || "Не удалось получить отчёт";
    }
  });

  /* ---------- Анализ продаж персонала ---------- */

  const saleMgrBody = document.getElementById("sale-mgr-body");
  const salePrBody = document.getElementById("sale-pr-body");
  const saleMsg = document.getElementById("sale-msg");

  /** @type {number | null} */
  let selectedMgrStaffId = null;

  function salePeriodQuery(extra) {
    const from = document.getElementById("sale-from").value;
    const to = document.getElementById("sale-to").value;
    const qs = [];
    if (from) qs.push(`from=${encodeURIComponent(from)}`);
    if (to) qs.push(`to=${encodeURIComponent(to)}`);
    if (extra && typeof extra.staffId === "number") {
      qs.push(`staffId=${extra.staffId}`);
    }
    return qs.length ? `?${qs.join("&")}` : "";
  }

  function clearMgrSelectionHighlight() {
    saleMgrBody.querySelectorAll("tr.row-selected").forEach((tr) => {
      tr.classList.remove("row-selected");
    });
  }

  async function loadSalesManagers() {
    tbodyClear(saleMgrBody);
    const q = salePeriodQuery();
    const rows = await apiJson(`reports/production/by-manager${q}`, {
      method: "GET",
    });
    rows.forEach((r) => {
      const tr = document.createElement("tr");
      tr.setAttribute("data-clickable", "true");
      tr.dataset.staffId = String(r.staffId);
      [
        r.staffFullName,
        r.totalOrders,
        r.completedOrders,
        r.cancelledOrders,
        formatMoney(r.totalRevenue),
      ].forEach((text) => {
        const td = document.createElement("td");
        td.textContent = text == null ? "—" : String(text);
        tr.appendChild(td);
      });
      if (selectedMgrStaffId === r.staffId) {
        tr.classList.add("row-selected");
      }
      saleMgrBody.appendChild(tr);
    });
  }

  async function loadSalesProducts() {
    tbodyClear(salePrBody);
    const extra =
      selectedMgrStaffId != null ? { staffId: selectedMgrStaffId } : {};
    const q = salePeriodQuery(extra);
    const rows = await apiJson(`reports/production/by-product${q}`, {
      method: "GET",
    });
    rows.forEach((r) => {
      trCells(salePrBody, [
        r.productName,
        r.totalOrders,
        r.totalQuantity,
        r.completedOrders,
        formatMoney(r.totalRevenue),
      ]);
    });
  }

  document.getElementById("sale-run").addEventListener("click", async () => {
    saleMsg.textContent = "";
    selectedMgrStaffId = null;
    clearMgrSelectionHighlight();
    try {
      await loadSalesManagers();
      await loadSalesProducts();
    } catch (e) {
      saleMsg.className = "msg error";
      saleMsg.textContent = e.message || "Ошибка загрузки анализа";
    }
  });

  document.getElementById("sale-clear-mgr").addEventListener("click", async () => {
    saleMsg.textContent = "";
    selectedMgrStaffId = null;
    clearMgrSelectionHighlight();
    try {
      await loadSalesProducts();
    } catch (e) {
      saleMsg.className = "msg error";
      saleMsg.textContent = e.message || "Ошибка загрузки";
    }
  });

  saleMgrBody.addEventListener("click", async (e) => {
    const tr = e.target.closest("tr[data-staff-id]");
    if (!tr) return;
    saleMsg.textContent = "";
    const sid = Number(tr.dataset.staffId);
    if (Number.isNaN(sid)) return;

    clearMgrSelectionHighlight();
    selectedMgrStaffId = sid;
    tr.classList.add("row-selected");

    try {
      await loadSalesProducts();
    } catch (e) {
      saleMsg.className = "msg error";
      saleMsg.textContent = e.message || "Ошибка загрузки";
    }
  });

  Promise.all([loadStaff(), loadNom()]).catch((e) => {
    alert(
      "Ошибка загрузки данных: " +
        (e.message || e) +
        ". Проверьте API и CORS."
    );
  });
})();
