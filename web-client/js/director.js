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
  const stAddBtn = document.getElementById("st-add");
  const stAddModal = document.getElementById("st-modal-add");
  const stAddName = document.getElementById("st-add-name");
  const stAddPosition = document.getElementById("st-add-position");
  const stAddMsg = document.getElementById("st-add-msg");
  const stEditModal = document.getElementById("st-modal-edit");
  const stEditId = document.getElementById("st-edit-id");
  const stEditName = document.getElementById("st-edit-name");
  const stEditPosition = document.getElementById("st-edit-position");
  const stEditUsername = document.getElementById("st-edit-username");
  const stEditPassword = document.getElementById("st-edit-password");
  const stEditMsg = document.getElementById("st-edit-msg");

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

  function maskedPassword(s) {
    const pwd = s.userAccount?.password;
    if (!pwd) return "—";
    return "••••••••";
  }

  function generateRandomPassword(length = 12) {
    const chars =
      "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
    let result = "";
    for (let i = 0; i < length; i += 1) {
      const idx = Math.floor(Math.random() * chars.length);
      result += chars[idx];
    }
    return result;
  }

  function generateRandomUsername(baseName, length = 6) {
    const source = (baseName || "").trim();
    const firstWord = source.split(/\s+/).find(Boolean) || "user";
    let suffix = "";
    for (let i = 0; i < length; i += 1) {
      suffix += String(Math.floor(Math.random() * 10));
    }
    return `${firstWord}${suffix}`;
  }

  function renderStaff() {
    tbodyClear(stBody);
    staffFiltered().forEach((s) => {
      const ua = s.userAccount;
      const tr = document.createElement("tr");
      [s.staffId, s.fullName, s.position, ua && ua.username ? ua.username : "—"].forEach((text) => {
        const td = document.createElement("td");
        td.textContent = text == null ? "—" : String(text);
        tr.appendChild(td);
      });

      const tdPwd = document.createElement("td");
      tdPwd.className = "cell-actions";
      const pwdLabel = document.createElement("span");
      pwdLabel.textContent = maskedPassword(s);
      tdPwd.appendChild(pwdLabel);
      if (s.userAccount?.password) {
        const copyPwdBtn = document.createElement("button");
        copyPwdBtn.type = "button";
        copyPwdBtn.className = "btn btn-secondary btn-inline";
        copyPwdBtn.textContent = "Копировать";
        copyPwdBtn.addEventListener("click", () => copyStaffPassword(s));
        tdPwd.appendChild(copyPwdBtn);
      }
      tr.appendChild(tdPwd);

      const tdAct = document.createElement("td");
      tdAct.className = "cell-actions";

      if (stMode.value === "working") {
        const editBtn = document.createElement("button");
        editBtn.type = "button";
        editBtn.className = "btn btn-secondary btn-inline";
        editBtn.textContent = "Редактировать";
        editBtn.addEventListener("click", () => openEditModal(s));
        tdAct.appendChild(editBtn);

        const fireBtn = document.createElement("button");
        fireBtn.type = "button";
        fireBtn.className = "btn btn-secondary btn-inline";
        fireBtn.textContent = "Уволить";
        fireBtn.addEventListener("click", () => fireStaff(s));
        tdAct.appendChild(fireBtn);
      } else {
        const reinstateBtn = document.createElement("button");
        reinstateBtn.type = "button";
        reinstateBtn.className = "btn btn-secondary btn-inline";
        reinstateBtn.textContent = "Вернуть в штат";
        reinstateBtn.addEventListener("click", () => reinstateStaff(s));
        tdAct.appendChild(reinstateBtn);
      }

      tr.appendChild(tdAct);
      stBody.appendChild(tr);
    });
  }

  function closeAddModal() {
    stAddModal.classList.remove("open");
    stAddModal.setAttribute("aria-hidden", "true");
    stAddMsg.textContent = "";
  }

  function openAddModal() {
    stAddName.value = "";
    stAddPosition.value = "";
    stAddMsg.textContent = "";
    stAddModal.classList.add("open");
    stAddModal.setAttribute("aria-hidden", "false");
    stAddName.focus();
  }

  function closeEditModal() {
    stEditModal.classList.remove("open");
    stEditModal.setAttribute("aria-hidden", "true");
    stEditMsg.textContent = "";
  }

  function openEditModal(staff) {
    stEditId.value = String(staff.staffId);
    stEditName.value = staff.fullName || "";
    stEditPosition.value = staff.position || "";
    stEditUsername.value = staff.userAccount?.username || "";
    stEditPassword.value = staff.userAccount?.password || "";
    stEditMsg.textContent = "";
    stEditModal.classList.add("open");
    stEditModal.setAttribute("aria-hidden", "false");
    stEditName.focus();
  }

  async function addStaff() {
    const fullName = stAddName.value.trim();
    const position = stAddPosition.value;
    if (!fullName || !position) {
      stAddMsg.textContent = "Укажите ФИО и должность.";
      return;
    }
    try {
      await apiJson("staff", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fullName, position }),
      });
      closeAddModal();
      await loadStaff();
    } catch (e) {
      stAddMsg.textContent = e.message || "Ошибка добавления сотрудника";
    }
  }

  async function saveEditedStaff() {
    const staffId = Number(stEditId.value);
    const fullName = stEditName.value.trim();
    const position = stEditPosition.value;
    const username = stEditUsername.value.trim();
    const password = stEditPassword.value.trim();

    if (Number.isNaN(staffId) || !fullName || !position) {
      stEditMsg.textContent = "Укажите корректные ФИО и должность.";
      return;
    }
    if (!username || !password) {
      stEditMsg.textContent = "Логин и пароль не могут быть пустыми.";
      return;
    }

    try {
      await apiJson(`staff/${staffId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          staffId,
          fullName,
          position,
          userAccount: {
            staffId,
            username,
            password,
          },
        }),
      });
      closeEditModal();
      await loadStaff();
    } catch (e) {
      stEditMsg.textContent = e.message || "Ошибка сохранения сотрудника";
    }
  }

  async function copyStaffPassword(staff) {
    const pwd = staff?.userAccount?.password;
    if (!pwd) {
      alert("У этого сотрудника нет доступного пароля для копирования.");
      return;
    }
    try {
      await navigator.clipboard.writeText(pwd);
    } catch {
      alert("Не удалось скопировать пароль в буфер обмена.");
    }
  }

  async function fireStaff(staff) {
    const ok = window.confirm(
      `Уволить сотрудника "${staff.fullName}"? Действие деактивирует его учётную запись.`
    );
    if (!ok) return;
    try {
      await apiJson(`staff/${staff.staffId}`, { method: "DELETE" });
      await loadStaff();
    } catch (e) {
      alert(e.message || "Не удалось уволить сотрудника");
    }
  }

  async function reinstateStaff(staff) {
    const ok = window.confirm(
      `Вернуть сотрудника "${staff.fullName}" в штат?`
    );
    if (!ok) return;
    try {
      await apiJson(`staff/${staff.staffId}/reinstate`, { method: "PUT" });
      await loadStaff();
    } catch (e) {
      alert(e.message || "Не удалось вернуть сотрудника в штат");
    }
  }

  function syncStaffModeControls() {
    stAddBtn.style.display = stMode.value === "working" ? "inline-flex" : "none";
  }

  async function loadStaff() {
    const endpoint =
      stMode.value === "fired" ? "staff/fired" : "staff";
    staffList = await apiJson(endpoint, { method: "GET" });
    rebuildPositionFilter();
    syncStaffModeControls();
    renderStaff();
  }

  stMode.addEventListener("change", loadStaff);
  stPosition.addEventListener("change", renderStaff);
  stSearch.addEventListener("input", renderStaff);
  document.getElementById("st-refresh").addEventListener("click", loadStaff);
  stAddBtn.addEventListener("click", openAddModal);
  document.getElementById("st-add-cancel").addEventListener("click", closeAddModal);
  document.getElementById("st-add-save").addEventListener("click", addStaff);
  stAddModal.addEventListener("click", (e) => {
    if (e.target === stAddModal) closeAddModal();
  });
  document.getElementById("st-edit-cancel").addEventListener("click", closeEditModal);
  document.getElementById("st-edit-save").addEventListener("click", saveEditedStaff);
  document
    .getElementById("st-edit-regen-username")
    .addEventListener("click", () => {
      stEditUsername.value = generateRandomUsername(stEditName.value);
    });
  document
    .getElementById("st-edit-regen-password")
    .addEventListener("click", () => {
      stEditPassword.value = generateRandomPassword();
    });
  stEditModal.addEventListener("click", (e) => {
    if (e.target === stEditModal) closeEditModal();
  });

  /* ---------- Номенклатура ---------- */

  /** @type {any[]} */
  let nomenclature = [];

  const nomBody = document.getElementById("nom-body");
  const nomMode = document.getElementById("nom-mode");
  const nomSearch = document.getElementById("nom-search");
  const nomCategory = document.getElementById("nom-category");
  const nomMin = document.getElementById("nom-min");
  const nomMax = document.getElementById("nom-max");
  const nomAddBtn = document.getElementById("nom-add");
  const nomAddModal = document.getElementById("nom-modal-add");
  const nomAddMsg = document.getElementById("nom-add-msg");
  const nomEditModal = document.getElementById("nom-modal-edit");
  const nomEditMsg = document.getElementById("nom-edit-msg");

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
      const tr = document.createElement("tr");
      [
        p.productId,
        p.name,
        p.category,
        p.color,
        p.dimensions,
        formatMoney(p.price),
      ].forEach((text) => {
        const td = document.createElement("td");
        td.textContent = text == null ? "—" : String(text);
        tr.appendChild(td);
      });

      const tdAct = document.createElement("td");
      tdAct.className = "cell-actions";
      if (nomMode.value === "produced") {
        const bEdit = document.createElement("button");
        bEdit.type = "button";
        bEdit.className = "btn btn-secondary btn-inline";
        bEdit.textContent = "Редактировать";
        bEdit.addEventListener("click", () => openEditNomModal(p));
        tdAct.appendChild(bEdit);

        const bDel = document.createElement("button");
        bDel.type = "button";
        bDel.className = "btn btn-secondary btn-inline";
        bDel.textContent = "Удалить";
        bDel.addEventListener("click", () => deleteNom(p));
        tdAct.appendChild(bDel);
      } else {
        const bReinstate = document.createElement("button");
        bReinstate.type = "button";
        bReinstate.className = "btn btn-secondary btn-inline";
        bReinstate.textContent = "Вернуть в производство";
        bReinstate.addEventListener("click", () => reinstateNom(p));
        tdAct.appendChild(bReinstate);
      }
      tr.appendChild(tdAct);
      nomBody.appendChild(tr);
    });
  }

  function nomPayloadFrom(prefix, productId) {
    const name = document.getElementById(`${prefix}-name`).value.trim();
    const category = document.getElementById(`${prefix}-category`).value.trim();
    const color = document.getElementById(`${prefix}-color`).value.trim();
    const dimensions = document.getElementById(`${prefix}-dimensions`).value.trim();
    const price = Number(document.getElementById(`${prefix}-price`).value);

    if (!name || !category || !color || !dimensions) {
      return { error: "Заполните все текстовые поля." };
    }
    if (Number.isNaN(price) || price < 0) {
      return { error: "Цена должна быть неотрицательным числом." };
    }
    return {
      data: {
        ...(typeof productId === "number" ? { productId } : {}),
        name,
        category,
        color,
        dimensions,
        price,
        isProduced: true,
      },
    };
  }

  function fillNomCategorySelect(selectEl, selectedValue) {
    const categories = [...nomCategory.options]
      .map((o) => o.value)
      .filter((v) => v);
    selectEl.innerHTML = "";
    categories.forEach((c) => {
      const opt = document.createElement("option");
      opt.value = c;
      opt.textContent = c;
      selectEl.appendChild(opt);
    });
    if (selectedValue && categories.includes(selectedValue)) {
      selectEl.value = selectedValue;
    } else if (categories.length > 0) {
      selectEl.value = categories[0];
    }
  }

  function closeNomAddModal() {
    nomAddModal.classList.remove("open");
    nomAddModal.setAttribute("aria-hidden", "true");
    nomAddMsg.textContent = "";
  }

  function openNomAddModal() {
    document.getElementById("nom-add-name").value = "";
    document.getElementById("nom-add-category").value = "";
    document.getElementById("nom-add-color").value = "";
    document.getElementById("nom-add-dimensions").value = "";
    document.getElementById("nom-add-price").value = "";
    nomAddMsg.textContent = "";
    nomAddModal.classList.add("open");
    nomAddModal.setAttribute("aria-hidden", "false");
  }

  function closeEditNomModal() {
    nomEditModal.classList.remove("open");
    nomEditModal.setAttribute("aria-hidden", "true");
    nomEditMsg.textContent = "";
  }

  function openEditNomModal(p) {
    document.getElementById("nom-edit-id").value = String(p.productId);
    document.getElementById("nom-edit-name").value = p.name || "";
    fillNomCategorySelect(
      document.getElementById("nom-edit-category"),
      p.category || ""
    );
    document.getElementById("nom-edit-color").value = p.color || "";
    document.getElementById("nom-edit-dimensions").value = p.dimensions || "";
    document.getElementById("nom-edit-price").value = String(p.price ?? "");
    nomEditMsg.textContent = "";
    nomEditModal.classList.add("open");
    nomEditModal.setAttribute("aria-hidden", "false");
  }

  async function addNom() {
    const payload = nomPayloadFrom("nom-add");
    if (payload.error) {
      nomAddMsg.textContent = payload.error;
      return;
    }
    try {
      await apiJson("nomenclature", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload.data),
      });
      closeNomAddModal();
      await loadNom();
    } catch (e) {
      nomAddMsg.textContent = e.message || "Ошибка добавления номенклатуры";
    }
  }

  async function saveEditNom() {
    const productId = Number(document.getElementById("nom-edit-id").value);
    if (Number.isNaN(productId)) {
      nomEditMsg.textContent = "Некорректный идентификатор номенклатуры.";
      return;
    }
    const payload = nomPayloadFrom("nom-edit", productId);
    if (payload.error) {
      nomEditMsg.textContent = payload.error;
      return;
    }
    try {
      await apiJson(`nomenclature/${productId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload.data),
      });
      closeEditNomModal();
      await loadNom();
    } catch (e) {
      nomEditMsg.textContent = e.message || "Ошибка обновления номенклатуры";
    }
  }

  async function deleteNom(p) {
    const ok = window.confirm(
      `Снять номенклатуру "${p.name}" с производства?`
    );
    if (!ok) return;
    try {
      await apiJson(`nomenclature/${p.productId}`, { method: "DELETE" });
      await loadNom();
    } catch (e) {
      alert(e.message || "Ошибка удаления номенклатуры");
    }
  }

  async function reinstateNom(p) {
    const ok = window.confirm(
      `Вернуть номенклатуру "${p.name}" в производство?`
    );
    if (!ok) return;
    try {
      await apiJson(`nomenclature/${p.productId}/reinstate`, {
        method: "PUT",
      });
      await loadNom();
    } catch (e) {
      alert(e.message || "Ошибка возврата номенклатуры в производство");
    }
  }

  function syncNomModeControls() {
    nomAddBtn.style.display =
      nomMode.value === "produced" ? "inline-flex" : "none";
  }

  async function loadNom() {
    const path =
      nomMode.value === "notproduced"
        ? "nomenclature/notproduced"
        : "nomenclature";
    nomenclature = await apiJson(path, { method: "GET" });
    nomRebuildCategories();
    syncNomModeControls();
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
  nomAddBtn.addEventListener("click", openNomAddModal);
  document.getElementById("nom-add-cancel").addEventListener("click", closeNomAddModal);
  document.getElementById("nom-add-save").addEventListener("click", addNom);
  nomAddModal.addEventListener("click", (e) => {
    if (e.target === nomAddModal) closeNomAddModal();
  });
  document.getElementById("nom-edit-cancel").addEventListener("click", closeEditNomModal);
  document.getElementById("nom-edit-save").addEventListener("click", saveEditNom);
  nomEditModal.addEventListener("click", (e) => {
    if (e.target === nomEditModal) closeEditNomModal();
  });

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
