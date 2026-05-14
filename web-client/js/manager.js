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

  // Вкладка "Номенклатура"

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

  // Вкладка "Заказы"

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
      const tr = document.createElement("tr");
      [
        o.orderId,
        formatRuDate(o.orderDate),
        o.product ? o.product.name : "—",
        o.quantity,
        o.status,
        o.client ? o.client.fullName : "—",
        o.staff ? o.staff.fullName : "—",
        formatMoney(o.totalPrice),
        formatRuDate(o.completionDate),
      ].forEach((text) => {
        const td = document.createElement("td");
        td.textContent = text == null ? "—" : String(text);
        tr.appendChild(td);
      });
      const tdAct = document.createElement("td");
      tdAct.className = "cell-actions";
      if (o.status !== "Выполнен" && o.status !== "Отменен") {
        const editBtn = document.createElement("button");
        editBtn.type = "button";
        editBtn.className = "btn btn-secondary btn-inline";
        editBtn.textContent = "Изменить";
        editBtn.addEventListener("click", () => openOrderModalEdit(o));
        tdAct.appendChild(editBtn);
      } else {
        tdAct.textContent = "—";
      }
      tr.appendChild(tdAct);
      ordBody.appendChild(tr);
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

  // Вкладка "Оформление и редактирование заказов"

  const ordModal = document.getElementById("ord-modal");
  const ordFormMsg = document.getElementById("ord-form-msg");
  const ordFormOrderId = document.getElementById("ord-form-order-id");
  const ordFormQty = document.getElementById("ord-form-qty");
  const ordFormTotal = document.getElementById("ord-form-total");
  const ordFormStatusWrap = document.getElementById("ord-form-status-wrap");
  const ordFormStatus = document.getElementById("ord-form-status");
  const ordModalTitle = document.getElementById("ord-modal-title");

  function nomenclatureComboboxOptions(query) {
    const q = (query || "").trim().toLowerCase();
    return [...nomenclature]
      .filter((p) => !q || (p.name || "").toLowerCase().includes(q))
      .sort((a, b) => (a.name || "").localeCompare(b.name || "", "ru"))
      .map((p) => ({
        id: p.productId,
        label: p.name || `№${p.productId}`,
      }));
  }

  function clientComboboxOptions(query) {
    const q = (query || "").trim().toLowerCase();
    return [...clients]
      .filter((c) => !q || (c.fullName || "").toLowerCase().includes(q))
      .sort((a, b) => (a.fullName || "").localeCompare(b.fullName || "", "ru"))
      .map((c) => {
        const id = c.id ?? c.clientId ?? c.Id;
        return {
          id,
          label: c.fullName || `Клиент ${id}`,
        };
      });
  }

  const ordProductCombo = createSearchCombobox({
    container: document.getElementById("ord-form-product-combobox"),
    inputId: "ord-form-product-input",
    placeholder: "Начните вводить название…",
    getOptions: nomenclatureComboboxOptions,
    onChange: updateOrderFormTotal,
  });

  const ordClientCombo = createSearchCombobox({
    container: document.getElementById("ord-form-client-combobox"),
    inputId: "ord-form-client-input",
    placeholder: "Начните вводить ФИО…",
    getOptions: clientComboboxOptions,
  });

  /** @type {null | { orderId: number; productId: number; productPrice: number | null; quantity: number; orderDate: string; staffId: number; completionDate: string | null; clientId: number; totalPrice: number; status: string }} */
  let editingOrderSnapshot = null;

  function getOrderProductId(o) {
    if (o.productId != null) return Number(o.productId);
    if (o.product && o.product.productId != null) return Number(o.product.productId);
    return NaN;
  }

  function getOrderClientId(o) {
    if (o.clientId != null) return Number(o.clientId);
    const c = o.client;
    if (!c) return NaN;
    if (c.id != null) return Number(c.id);
    if (c.clientId != null) return Number(c.clientId);
    if (c.Id != null) return Number(c.Id);
    return NaN;
  }

  function getOrderStaffId(o) {
    if (o.staffId != null) return Number(o.staffId);
    if (o.staff && o.staff.staffId != null) return Number(o.staff.staffId);
    return NaN;
  }

  function isOrderContentEditableByStatus(status) {
    return status === "В обработке";
  }

  function allowedOrderStatuses(orderId, currentStatus) {
    if (!orderId) return ["В обработке"];
    if (currentStatus === "В обработке") {
      return ["В обработке", "В производстве", "Отменен"];
    }
    if (currentStatus === "В производстве") {
      return ["В производстве", "Выполнен", "Отменен"];
    }
    return [currentStatus];
  }

  function updateOrderFormTotal() {
    const pid = Number(ordProductCombo.getValue());
    const qty = Number(ordFormQty.value);
    const p = nomenclature.find((x) => Number(x.productId) === pid);
    let line = null;
    if (p && qty > 0) {
      line = p.price * qty;
    } else if (
      editingOrderSnapshot &&
      Number(editingOrderSnapshot.productId) === pid &&
      editingOrderSnapshot.productPrice != null &&
      qty > 0
    ) {
      line = editingOrderSnapshot.productPrice * qty;
    }
    ordFormTotal.textContent = line != null ? formatMoney(line) : "—";
  }

  function applyOrderFormEditability() {
    const snap = editingOrderSnapshot;
    const contentEditable = !snap || isOrderContentEditableByStatus(snap.status);
    ordProductCombo.setReadOnly(!contentEditable);
    ordClientCombo.setReadOnly(!contentEditable);
    ordFormQty.disabled = !contentEditable;
    const statusEditable =
      snap &&
      snap.status !== "Выполнен" &&
      snap.status !== "Отменен";
    ordFormStatus.disabled = !statusEditable;
  }

  function fillStatusSelect(orderId, currentStatus) {
    ordFormStatus.innerHTML = "";
    const allowed = allowedOrderStatuses(orderId, currentStatus);
    allowed.forEach((s) => {
      const opt = document.createElement("option");
      opt.value = s;
      opt.textContent = s;
      ordFormStatus.appendChild(opt);
    });
    if (allowed.includes(currentStatus)) {
      ordFormStatus.value = currentStatus;
    } else {
      ordFormStatus.value = allowed[0];
    }
  }

  function closeOrderModal() {
    ordModal.classList.remove("open");
    ordModal.setAttribute("aria-hidden", "true");
    ordFormMsg.textContent = "";
    editingOrderSnapshot = null;
    ordProductCombo.close();
    ordClientCombo.close();
  }

  async function openOrderModalAdd() {
    ordFormMsg.textContent = "";
    editingOrderSnapshot = null;
    ordModalTitle.textContent = "Новый заказ";
    ordFormOrderId.value = "";
    ordFormStatusWrap.style.display = "none";

    if (!nomenclature.length) {
      try {
        await loadNom();
      } catch {
        alert("Не удалось загрузить номенклатуру.");
        return;
      }
    }
    if (!clients.length) {
      try {
        await loadCl();
      } catch {
        alert("Не удалось загрузить клиентов.");
        return;
      }
    }

    ordProductCombo.clear();
    ordClientCombo.clear();
    ordFormQty.value = "1";
    updateOrderFormTotal();
    applyOrderFormEditability();

    ordModal.classList.add("open");
    ordModal.setAttribute("aria-hidden", "false");
    ordProductCombo.focus();
  }

  async function openOrderModalEdit(o) {
    if (o.status === "Выполнен" || o.status === "Отменен") {
      alert("Редактирование невозможно: заказ уже завершён или отменён.");
      return;
    }
    ordFormMsg.textContent = "";
    const pp = o.product && o.product.price != null ? Number(o.product.price) : null;
    editingOrderSnapshot = {
      orderId: o.orderId,
      productId: getOrderProductId(o),
      productPrice: Number.isFinite(pp) ? pp : null,
      quantity: o.quantity,
      orderDate: o.orderDate,
      staffId: getOrderStaffId(o),
      completionDate: o.completionDate ?? null,
      clientId: getOrderClientId(o),
      totalPrice: o.totalPrice,
      status: o.status,
    };

    if (!nomenclature.length) {
      try {
        await loadNom();
      } catch {
        alert("Не удалось загрузить номенклатуру.");
        return;
      }
    }
    if (!clients.length) {
      try {
        await loadCl();
      } catch {
        alert("Не удалось загрузить клиентов.");
        return;
      }
    }

    ordModalTitle.textContent = "Редактировать заказ";
    ordFormOrderId.value = String(o.orderId);
    ordFormStatusWrap.style.display = "";

    const pname = o.product && o.product.name ? o.product.name : "";
    const cname = o.client && o.client.fullName ? o.client.fullName : "";
    ordProductCombo.setValue(editingOrderSnapshot.productId, pname);
    ordClientCombo.setValue(editingOrderSnapshot.clientId, cname);
    ordFormQty.value = String(editingOrderSnapshot.quantity);

    fillStatusSelect(editingOrderSnapshot.orderId, editingOrderSnapshot.status);
    updateOrderFormTotal();
    applyOrderFormEditability();

    ordModal.classList.add("open");
    ordModal.setAttribute("aria-hidden", "false");
  }

  ordFormQty.addEventListener("input", updateOrderFormTotal);

  document.getElementById("ord-add").addEventListener("click", () => {
    openOrderModalAdd();
  });

  document.getElementById("ord-form-cancel").addEventListener("click", closeOrderModal);
  ordModal.addEventListener("click", (e) => {
    if (e.target === ordModal) closeOrderModal();
  });

  document.getElementById("ord-form-save").addEventListener("click", async () => {
    ordFormMsg.textContent = "";
    const snap = editingOrderSnapshot;
    const isEdit = Boolean(snap && ordFormOrderId.value);

    let productId = Number(ordProductCombo.getValue());
    let clientId = Number(ordClientCombo.getValue());
    let qty = Number(ordFormQty.value);
    let status = "В обработке";

    if (isEdit && snap) {
      status = ordFormStatus.value;
      if (!isOrderContentEditableByStatus(snap.status)) {
        productId = snap.productId;
        clientId = snap.clientId;
        qty = snap.quantity;
      }
    }

    if (!productId || Number.isNaN(productId)) {
      ordFormMsg.textContent = "Выберите номенклатуру.";
      return;
    }
    if (!clientId || Number.isNaN(clientId)) {
      ordFormMsg.textContent = "Выберите клиента.";
      return;
    }
    if (!qty || qty < 1 || Number.isNaN(qty)) {
      ordFormMsg.textContent = "Укажите количество больше нуля.";
      return;
    }

    const product = nomenclature.find((p) => Number(p.productId) === productId);
    let unitPrice = product ? product.price : null;
    if (
      unitPrice == null &&
      snap &&
      Number(snap.productId) === productId &&
      snap.productPrice != null
    ) {
      unitPrice = snap.productPrice;
    }
    if (unitPrice == null || unitPrice < 0) {
      ordFormMsg.textContent = "Не удалось определить цену выбранной номенклатуры.";
      return;
    }

    const allowed = isEdit && snap ? allowedOrderStatuses(snap.orderId, snap.status) : ["В обработке"];
    if (!allowed.includes(status)) {
      ordFormMsg.textContent = "Недопустимый статус для текущего состояния заказа.";
      return;
    }

    const totalPrice =
      isEdit && snap && !isOrderContentEditableByStatus(snap.status)
        ? snap.totalPrice
        : unitPrice * qty;

    try {
      if (!isEdit) {
        await apiJson("orders", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            orderId: 0,
            productId,
            quantity: qty,
            staffId: user.staffId,
            clientId,
            totalPrice,
            status: "В обработке",
          }),
        });
      } else if (isEdit && snap) {
        await apiJson(`orders/${snap.orderId}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            orderId: snap.orderId,
            productId,
            quantity: qty,
            orderDate: snap.orderDate,
            staffId: snap.staffId,
            completionDate: snap.completionDate,
            clientId,
            totalPrice,
            status,
          }),
        });
      }
      closeOrderModal();
      await loadOrd();
    } catch (e) {
      ordFormMsg.textContent = e.message || "Ошибка сохранения заказа";
    }
  });

  // Вкладка "Клиенты"

  const clBody = document.getElementById("cl-body");
  const clSearch = document.getElementById("cl-search");
  const clModal = document.getElementById("cl-modal");
  const clModalTitle = document.getElementById("cl-modal-title");
  const clEditId = document.getElementById("cl-edit-id");
  const clEditRegistration = document.getElementById("cl-edit-registration");

  function getClientId(c) {
    if (c == null) return null;
    if (c.id != null) return Number(c.id);
    if (c.Id != null) return Number(c.Id);
    return null;
  }

  function closeClModal() {
    clModal.classList.remove("open");
    clModal.setAttribute("aria-hidden", "true");
  }

  function openClModalAdd() {
    clModalTitle.textContent = "Новый клиент";
    clEditId.value = "";
    clEditRegistration.value = "";
    document.getElementById("cl-fn").value = "";
    document.getElementById("cl-phone").value = "";
    document.getElementById("cl-email").value = "";
    document.getElementById("cl-address").value = "";
    document.getElementById("cl-modal-msg").textContent = "";
    clModal.classList.add("open");
    clModal.setAttribute("aria-hidden", "false");
  }

  function openClModalEdit(c) {
    const id = getClientId(c);
    if (id == null || Number.isNaN(id)) {
      alert("Не удалось определить ID клиента.");
      return;
    }
    clModalTitle.textContent = "Редактировать клиента";
    clEditId.value = String(id);
    const reg = c.registrationDate;
    clEditRegistration.value =
      reg == null || reg === ""
        ? ""
        : typeof reg === "string"
          ? reg
          : new Date(reg).toISOString();
    document.getElementById("cl-fn").value = c.fullName || "";
    document.getElementById("cl-phone").value = c.phone != null ? String(c.phone) : "";
    document.getElementById("cl-email").value = c.email || "";
    document.getElementById("cl-address").value = c.address || "";
    document.getElementById("cl-modal-msg").textContent = "";
    clModal.classList.add("open");
    clModal.setAttribute("aria-hidden", "false");
  }

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
      const tr = document.createElement("tr");
      [
        c.fullName,
        String(c.phone),
        c.email || "—",
        c.address || "—",
        formatRuDate(c.registrationDate),
      ].forEach((text) => {
        const td = document.createElement("td");
        td.textContent = text == null ? "—" : String(text);
        tr.appendChild(td);
      });
      const tdAct = document.createElement("td");
      tdAct.className = "cell-actions";
      const editBtn = document.createElement("button");
      editBtn.type = "button";
      editBtn.className = "btn btn-secondary btn-inline";
      editBtn.textContent = "Изменить";
      editBtn.addEventListener("click", () => openClModalEdit(c));
      tdAct.appendChild(editBtn);
      tr.appendChild(tdAct);
      clBody.appendChild(tr);
    });
  }

  async function loadCl() {
    clients = await apiJson("clients", { method: "GET" });
    renderCl();
  }

  clSearch.addEventListener("input", renderCl);
  document.getElementById("cl-refresh").addEventListener("click", loadCl);

  document.getElementById("cl-add").addEventListener("click", () => {
    openClModalAdd();
  });
  document.getElementById("cl-cancel").addEventListener("click", closeClModal);
  clModal.addEventListener("click", (e) => {
    if (e.target === clModal) closeClModal();
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
      msg.textContent = "Заполните все поля.";
      return;
    }
    const editIdStr = clEditId.value.trim();
    const isEdit = Boolean(editIdStr);
    try {
      if (!isEdit) {
        const body = {
          fullName,
          phone,
          email,
          address,
          registrationDate: new Date().toISOString(),
        };
        await apiJson("clients", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(body),
        });
      } else {
        const id = Number(editIdStr);
        const regRaw = clEditRegistration.value.trim();
        const registrationDate =
          regRaw || new Date().toISOString();
        const body = {
          id,
          fullName,
          phone,
          email,
          address,
          registrationDate,
        };
        await apiJson(`clients/${id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(body),
        });
      }
      closeClModal();
      await loadCl();
    } catch (e) {
      msg.textContent = e.message || "Ошибка сохранения";
    }
  });

  // Вкладка "Отчёты о производстве"

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

  // Старт

  Promise.all([loadNom(), loadOrd(), loadCl()]).catch((e) => {
    alert(
      "Ошибка загрузки данных: " +
        (e.message || e) +
        ". Проверьте, что сервер запущен и доступен."
    );
  });
})();
