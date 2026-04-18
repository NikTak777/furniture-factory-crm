(function () {
  const user = requireManagerPage();
  if (!user) return;

  document.getElementById("user-line").textContent =
    `${user.fullName} · ${user.position}`;

  /** @type {any[]} */
  let nomenclature = [];
  /** @type {any[]} */
  let orders = [];
  /** @type {any[]} */
  let clients = [];

  const panels = {
    nom: document.getElementById("panel-nom"),
    orders: document.getElementById("panel-orders"),
    clients: document.getElementById("panel-clients"),
    reports: document.getElementById("panel-reports"),
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

  /* ---------- Номенклатура ---------- */

  const nomBody = document.getElementById("nom-body");
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
        p.name,
        p.category,
        p.color,
        p.dimensions,
        formatMoney(p.price),
      ]);
    });
  }

  async function loadNom() {
    nomenclature = await apiJson("nomenclature", { method: "GET" });
    nomRebuildCategories();
    renderNom();
  }

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

  /* ---------- Заказы ---------- */

  const ordBody = document.getElementById("ord-body");
  const ordStatus = document.getElementById("ord-status");
  const ordProduct = document.getElementById("ord-product");
  const ordClient = document.getElementById("ord-client");

  function ordRebuildStatuses() {
    const prev = ordStatus.value;
    const set = new Set();
    orders.forEach((o) => {
      if (o.status) set.add(o.status);
    });
    const list = Array.from(set).sort((a, b) => a.localeCompare(b, "ru"));
    ordStatus.innerHTML = "";
    const all = document.createElement("option");
    all.value = "";
    all.textContent = "Все статусы";
    ordStatus.appendChild(all);
    list.forEach((s) => {
      const opt = document.createElement("option");
      opt.value = s;
      opt.textContent = s;
      ordStatus.appendChild(opt);
    });
    if (prev && [...ordStatus.options].some((o) => o.value === prev)) {
      ordStatus.value = prev;
    }
  }

  function ordFiltered() {
    const st = ordStatus.value;
    const pq = ordProduct.value.trim().toLowerCase();
    const cq = ordClient.value.trim().toLowerCase();
    return orders.filter((o) => {
      if (st && o.status !== st) return false;
      const pname = (o.product && o.product.name ? o.product.name : "").toLowerCase();
      if (pq && !pname.includes(pq)) return false;
      const cname = (o.client && o.client.fullName ? o.client.fullName : "").toLowerCase();
      if (cq && !cname.includes(cq)) return false;
      return true;
    });
  }

  function renderOrd() {
    tbodyClear(ordBody);
    ordFiltered().forEach((o) => {
      trCells(ordBody, [
        o.orderId,
        formatRuDate(o.orderDate),
        o.product ? o.product.name : "—",
        o.quantity,
        o.status,
        o.client ? o.client.fullName : "—",
        o.staff ? o.staff.fullName : "—",
        formatMoney(o.totalPrice),
        formatRuDate(o.completionDate),
      ]);
    });
  }

  async function loadOrd() {
    orders = await apiJson("orders", { method: "GET" });
    ordRebuildStatuses();
    renderOrd();
  }

  ["input", "change"].forEach((ev) => {
    ordStatus.addEventListener(ev, renderOrd);
    ordProduct.addEventListener(ev, renderOrd);
    ordClient.addEventListener(ev, renderOrd);
  });

  document.getElementById("ord-clear").addEventListener("click", () => {
    ordStatus.value = "";
    ordProduct.value = "";
    ordClient.value = "";
    renderOrd();
  });
  document.getElementById("ord-refresh").addEventListener("click", loadOrd);

  /* ---------- Клиенты ---------- */

  const clBody = document.getElementById("cl-body");
  const clSearch = document.getElementById("cl-search");

  function clFiltered() {
    const q = clSearch.value.trim().toLowerCase();
    if (!q) return clients.slice();
    return clients.filter((c) => {
      const blob = [
        c.fullName,
        String(c.phone),
        c.email || "",
        c.address || "",
      ]
        .join(" ")
        .toLowerCase();
      return blob.includes(q);
    });
  }

  function renderCl() {
    tbodyClear(clBody);
    clFiltered().forEach((c) => {
      trCells(clBody, [
        c.fullName,
        String(c.phone),
        c.email || "—",
        c.address || "—",
        formatRuDate(c.registrationDate),
      ]);
    });
  }

  async function loadCl() {
    clients = await apiJson("clients", { method: "GET" });
    renderCl();
  }

  clSearch.addEventListener("input", renderCl);
  document.getElementById("cl-refresh").addEventListener("click", loadCl);

  const modal = document.getElementById("cl-modal");
  document.getElementById("cl-add").addEventListener("click", () => {
    document.getElementById("cl-fn").value = "";
    document.getElementById("cl-phone").value = "";
    document.getElementById("cl-email").value = "";
    document.getElementById("cl-address").value = "";
    document.getElementById("cl-modal-msg").textContent = "";
    modal.classList.add("open");
    modal.setAttribute("aria-hidden", "false");
  });
  document.getElementById("cl-cancel").addEventListener("click", () => {
    modal.classList.remove("open");
    modal.setAttribute("aria-hidden", "true");
  });
  modal.addEventListener("click", (e) => {
    if (e.target === modal) {
      modal.classList.remove("open");
      modal.setAttribute("aria-hidden", "true");
    }
  });

  document.getElementById("cl-save").addEventListener("click", async () => {
    const msg = document.getElementById("cl-modal-msg");
    msg.textContent = "";
    const fullName = document.getElementById("cl-fn").value.trim();
    const phoneRaw = document.getElementById("cl-phone").value.trim();
    const email = document.getElementById("cl-email").value.trim();
    const address = document.getElementById("cl-address").value.trim();
    const phone = Number(phoneRaw.replace(/\D/g, ""));
    if (!fullName || !phone || !email || !address) {
      msg.textContent = "Заполните все поля; телефон — цифры.";
      return;
    }
    const body = {
      fullName,
      phone,
      email,
      address,
      registrationDate: new Date().toISOString(),
    };
    try {
      await apiJson("clients", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      modal.classList.remove("open");
      modal.setAttribute("aria-hidden", "true");
      await loadCl();
    } catch (e) {
      msg.textContent = e.message || "Ошибка сохранения";
    }
  });

  /* ---------- Отчёт ---------- */

  const repStats = document.getElementById("rep-stats");
  const repBody = document.getElementById("rep-body");
  const repMsg = document.getElementById("rep-msg");

  document.getElementById("rep-run").addEventListener("click", async () => {
    repMsg.textContent = "";
    tbodyClear(repBody);
    repStats.style.display = "none";
    repStats.innerHTML = "";
    if (typeof destroyProductionReportCharts === "function") {
      destroyProductionReportCharts();
    }
    const mgrCharts = document.getElementById("mgr-charts-wrap");
    if (mgrCharts) mgrCharts.style.display = "none";

    const from = document.getElementById("rep-from").value;
    const to = document.getElementById("rep-to").value;
    const qs = [];
    if (from) qs.push(`from=${encodeURIComponent(from)}`);
    if (to) qs.push(`to=${encodeURIComponent(to)}`);
    const path = qs.length ? `reports/production?${qs.join("&")}` : "reports/production";

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
        repStats.appendChild(box);
      });
      repStats.style.display = "grid";

      (data.orders || []).forEach((o) => {
        trCells(repBody, [
          o.orderId,
          formatRuDate(o.orderDate),
          o.productName,
          o.quantity,
          o.status,
          formatMoney(o.totalPrice),
        ]);
      });

      if (typeof renderProductionReportCharts === "function") {
        renderProductionReportCharts("mgr-charts-wrap", data.orders || []);
      }
    } catch (e) {
      repMsg.className = "msg error";
      repMsg.textContent = e.message || "Не удалось получить отчёт";
    }
  });

  /* ---------- Старт ---------- */

  Promise.all([loadNom(), loadOrd(), loadCl()]).catch((e) => {
    alert(
      "Ошибка загрузки данных: " +
        (e.message || e) +
        ". Проверьте, что API запущен и CORS включён."
    );
  });
})();
