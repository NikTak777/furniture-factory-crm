(function () {
  const user = requireWarehousePage();
  if (!user) return;

  document.getElementById("user-line").textContent =
    `${user.fullName} · ${user.position}`;

  /** @type {any[]} */
  let materials = [];
  /** @type {any[]} */
  let orders = [];
  /** @type {Map<number, string>} */
  let staffNames = new Map();

  const panels = {
    stock: document.getElementById("panel-stock"),
    orders: document.getElementById("panel-orders"),
    needs: document.getElementById("panel-needs"),
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

  function fillMaterialSelect(sel) {
    sel.innerHTML = "";
    materials.forEach((m) => {
      const opt = document.createElement("option");
      opt.value = String(m.materialId);
      opt.textContent = `${m.materialId} — ${m.materialName}`;
      sel.appendChild(opt);
    });
  }

  // Вкладка "Учёт сырья на складе"

  const stBody = document.getElementById("st-body");
  const stSearch = document.getElementById("st-search");
  const stUnit = document.getElementById("st-unit");
  const stMin = document.getElementById("st-min");
  const stMax = document.getElementById("st-max");

  function rebuildUnits() {
    const prev = stUnit.value;
    const set = new Set();
    materials.forEach((m) => {
      if (m.unit) set.add(m.unit);
    });
    const list = Array.from(set).sort((a, b) => a.localeCompare(b, "ru"));
    stUnit.innerHTML = "";
    const all = document.createElement("option");
    all.value = "";
    all.textContent = "Все единицы";
    stUnit.appendChild(all);
    list.forEach((u) => {
      const opt = document.createElement("option");
      opt.value = u;
      opt.textContent = u;
      stUnit.appendChild(opt);
    });
    if (prev && [...stUnit.options].some((o) => o.value === prev)) {
      stUnit.value = prev;
    }
  }

  function fillMatUnitSelect(selectedUnit) {
    const matUnit = document.getElementById("mat-unit");
    const unitValues = [...stUnit.options]
      .map((o) => o.value)
      .filter((v) => v);

    matUnit.innerHTML = "";
    unitValues.forEach((u) => {
      const opt = document.createElement("option");
      opt.value = u;
      opt.textContent = u;
      matUnit.appendChild(opt);
    });

    if (selectedUnit && unitValues.includes(selectedUnit)) {
      matUnit.value = selectedUnit;
    } else if (unitValues.length > 0) {
      matUnit.value = unitValues[0];
    }
  }

  function stockFiltered() {
    const q = stSearch.value.trim().toLowerCase();
    const unit = stUnit.value;
    const min = stMin.value !== "" ? Number(stMin.value) : null;
    const max = stMax.value !== "" ? Number(stMax.value) : null;
    return materials.filter((m) => {
      const name = (m.materialName || "").toLowerCase();
      if (q && !name.includes(q)) return false;
      if (unit && m.unit !== unit) return false;
      if (min != null && !Number.isNaN(min) && m.quantityAvailable < min)
        return false;
      if (max != null && !Number.isNaN(max) && m.quantityAvailable > max)
        return false;
      return true;
    });
  }

  function renderStock() {
    tbodyClear(stBody);
    stockFiltered().forEach((m) => {
      const tr = document.createElement("tr");
      [m.materialId, m.materialName, m.unit, m.quantityAvailable].forEach(
        (text) => {
          const td = document.createElement("td");
          td.textContent = text == null ? "—" : String(text);
          tr.appendChild(td);
        }
      );
      const tdAct = document.createElement("td");
      tdAct.className = "cell-actions";
      const bEd = document.createElement("button");
      bEd.type = "button";
      bEd.className = "btn btn-secondary btn-inline";
      bEd.textContent = "Изменить";
      bEd.addEventListener("click", () => openMatModal(m));
      const bDel = document.createElement("button");
      bDel.type = "button";
      bDel.className = "btn btn-secondary btn-inline";
      bDel.textContent = "Удалить";
      bDel.addEventListener("click", () => deleteMaterial(m));
      tdAct.appendChild(bEd);
      tdAct.appendChild(bDel);
      tr.appendChild(tdAct);
      stBody.appendChild(tr);
    });
  }

  async function loadMaterials() {
    materials = await apiJson("materials", { method: "GET" });
    rebuildUnits();
    renderStock();
  }

  ["input", "change"].forEach((ev) => {
    stSearch.addEventListener(ev, renderStock);
    stUnit.addEventListener(ev, renderStock);
    stMin.addEventListener(ev, renderStock);
    stMax.addEventListener(ev, renderStock);
  });

  document.getElementById("st-clear").addEventListener("click", () => {
    stSearch.value = "";
    stMin.value = "";
    stMax.value = "";
    stUnit.value = "";
    renderStock();
  });
  document.getElementById("st-refresh").addEventListener("click", loadMaterials);

  const matModal = document.getElementById("mat-modal");
  function closeMatModal() {
    matModal.classList.remove("open");
    matModal.setAttribute("aria-hidden", "true");
    document.getElementById("mat-modal-msg").textContent = "";
  }

  function openMatModal(existing) {
    document.getElementById("mat-modal-msg").textContent = "";
    document.getElementById("mat-modal-title").textContent = existing
      ? "Изменить сырьё"
      : "Новое сырьё";
    document.getElementById("mat-edit-id").value = existing
      ? String(existing.materialId)
      : "";
    document.getElementById("mat-name").value = existing
      ? existing.materialName || ""
      : "";
    fillMatUnitSelect(existing ? existing.unit || "" : "");
    document.getElementById("mat-qty").value = existing
      ? String(existing.quantityAvailable ?? 0)
      : "0";
    matModal.classList.add("open");
    matModal.setAttribute("aria-hidden", "false");
  }

  document.getElementById("st-add").addEventListener("click", () =>
    openMatModal(null)
  );
  document.getElementById("mat-cancel").addEventListener("click", closeMatModal);
  matModal.addEventListener("click", (e) => {
    if (e.target === matModal) closeMatModal();
  });

  document.getElementById("mat-save").addEventListener("click", async () => {
    const msg = document.getElementById("mat-modal-msg");
    msg.textContent = "";
    const idRaw = document.getElementById("mat-edit-id").value;
    const name = document.getElementById("mat-name").value.trim();
    const unit = document.getElementById("mat-unit").value.trim();
    const qty = Number(document.getElementById("mat-qty").value);
    if (!name || !unit || Number.isNaN(qty) || qty < 0) {
      msg.textContent = "Заполните наименование, единицу и неотрицательное количество.";
      return;
    }
    try {
      if (!idRaw) {
        await apiJson("materials", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            materialId: 0,
            materialName: name,
            unit,
            quantityAvailable: qty,
          }),
        });
      } else {
        const id = Number(idRaw);
        await apiJson(`materials/${id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            materialId: id,
            materialName: name,
            unit,
            quantityAvailable: qty,
          }),
        });
      }
      closeMatModal();
      await loadMaterials();
      await loadOrders();
    } catch (e) {
      msg.textContent = e.message || "Ошибка сохранения";
    }
  });

  async function deleteMaterial(m) {
    if (
      !confirm(
        `Удалить сырьё «${m.materialName}»? Операция возможна только если нет блокирующих связей.`
      )
    ) {
      return;
    }
    try {
      await apiJson(`materials/${m.materialId}`, { method: "DELETE" });
      await loadMaterials();
      await loadOrders();
    } catch (e) {
      alert(e.message || "Не удалось удалить");
    }
  }

  // Вкладка "Оформление закупки сырья"

  const moBody = document.getElementById("mo-body");
  const moSup = document.getElementById("mo-sup");
  const moMat = document.getElementById("mo-mat");
  const moStaffSel = document.getElementById("mo-staff");
  const moStatusSel = document.getElementById("mo-status");
  const moFrom = document.getElementById("mo-from");
  const moTo = document.getElementById("mo-to");

  async function resolveStaffMap(ids) {
    const staffList = await apiJson("staff", { method: "GET" });
    const map = new Map(
      staffList.map((s) => [s.staffId, s.fullName])
    );
    const missing = [...new Set(ids)].filter((id) => !map.has(id));
    await Promise.all(
      missing.map(async (id) => {
        try {
          const s = await apiJson(`staff/${id}`, { method: "GET" });
          map.set(id, s.fullName);
        } catch {
          map.set(id, `#${id}`);
        }
      })
    );
    return map;
  }

  function materialNameById(mid) {
    const m = materials.find((x) => x.materialId === mid);
    return m ? m.materialName : `Материал ${mid}`;
  }

  function ordersFiltered() {
    const fs = moSup.value.trim().toLowerCase();
    const fm = moMat.value.trim().toLowerCase();
    const fst = moStaffSel.value;
    const st = moStatusSel.value;

    return orders.filter((o) => {
      const sup = (o.supplier || "").toLowerCase();
      if (fs && !sup.includes(fs)) return false;
      const mn = materialNameById(o.materialId).toLowerCase();
      if (fm && !mn.includes(fm)) return false;
      const staffLabel = staffNames.get(o.staffId) || "";
      if (fst && fst !== "__all__" && staffLabel !== fst) return false;
      if (st && st !== "__all__" && o.status !== st) return false;
      if (moFrom.value || moTo.value) {
        const odStr = o.orderDate
          ? String(o.orderDate).slice(0, 10)
          : "";
        if (!odStr) return false;
        if (moFrom.value && odStr < moFrom.value) return false;
        if (moTo.value && odStr > moTo.value) return false;
      }
      return true;
    });
  }

  function rebuildOrderFilters() {
    const prevSt = moStaffSel.value;
    const prevStat = moStatusSel.value;

    const names = [
      ...new Set(orders.map((o) => staffNames.get(o.staffId)).filter(Boolean)),
    ].sort((a, b) => a.localeCompare(b, "ru"));
    moStaffSel.innerHTML = "";
    const oAll = document.createElement("option");
    oAll.value = "__all__";
    oAll.textContent = "Все оформители";
    moStaffSel.appendChild(oAll);
    names.forEach((n) => {
      const opt = document.createElement("option");
      opt.value = n;
      opt.textContent = n;
      moStaffSel.appendChild(opt);
    });
    if (prevSt && [...moStaffSel.options].some((o) => o.value === prevSt)) {
      moStaffSel.value = prevSt;
    }

    const stats = [
      ...new Set(orders.map((o) => o.status).filter(Boolean)),
    ].sort((a, b) => a.localeCompare(b, "ru"));
    moStatusSel.innerHTML = "";
    const sAll = document.createElement("option");
    sAll.value = "__all__";
    sAll.textContent = "Все статусы";
    moStatusSel.appendChild(sAll);
    stats.forEach((s) => {
      const opt = document.createElement("option");
      opt.value = s;
      opt.textContent = s;
      moStatusSel.appendChild(opt);
    });
    if (prevStat && [...moStatusSel.options].some((o) => o.value === prevStat)) {
      moStatusSel.value = prevStat;
    }
  }

  function renderOrders() {
    tbodyClear(moBody);
    ordersFiltered().forEach((o) => {
      const tr = document.createElement("tr");
      const cells = [
        o.materialOrderId,
        formatRuDate(o.orderDate),
        o.supplier,
        materialNameById(o.materialId),
        o.quantity,
        staffNames.get(o.staffId) || String(o.staffId),
        o.status,
      ];
      cells.forEach((text) => {
        const td = document.createElement("td");
        td.textContent = text == null ? "—" : String(text);
        tr.appendChild(td);
      });
      const tdAct = document.createElement("td");
      tdAct.className = "cell-actions";
      if (o.status === "Ожидает поставки") {
        const b = document.createElement("button");
        b.type = "button";
        b.className = "btn btn-secondary btn-inline";
        b.textContent = "Изменить";
        b.addEventListener("click", () => openEditOrderModal(o));
        tdAct.appendChild(b);
      } else {
        tdAct.textContent = "—";
      }
      tr.appendChild(tdAct);
      moBody.appendChild(tr);
    });
  }

  async function loadOrders() {
    const raw = await apiJson("materialorders", { method: "GET" });
    staffNames = await resolveStaffMap(raw.map((o) => o.staffId));
    orders = raw;
    rebuildOrderFilters();
    renderOrders();
  }

  ["input", "change"].forEach((ev) => {
    moSup.addEventListener(ev, renderOrders);
    moMat.addEventListener(ev, renderOrders);
    moStaffSel.addEventListener(ev, renderOrders);
    moStatusSel.addEventListener(ev, renderOrders);
    moFrom.addEventListener(ev, renderOrders);
    moTo.addEventListener(ev, renderOrders);
  });

  document.getElementById("mo-clear").addEventListener("click", () => {
    moSup.value = "";
    moMat.value = "";
    moFrom.value = "";
    moTo.value = "";
    moStaffSel.value = "__all__";
    moStatusSel.value = "__all__";
    renderOrders();
  });
  document.getElementById("mo-refresh").addEventListener("click", async () => {
    await loadMaterials();
    await loadOrders();
  });

  const moNewModal = document.getElementById("mo-modal-new");
  document.getElementById("mo-add").addEventListener("click", async () => {
    document.getElementById("mo-new-msg").textContent = "";
    await loadMaterials();
    if (!materials.length) {
      alert("Сначала добавьте сырьё на вкладке «Учёт сырья на складе».");
      return;
    }
    fillMaterialSelect(document.getElementById("mo-new-mat"));
    document.getElementById("mo-new-sup").value = "";
    document.getElementById("mo-new-qty").value = "1";
    moNewModal.classList.add("open");
    moNewModal.setAttribute("aria-hidden", "false");
  });
  document.getElementById("mo-new-cancel").addEventListener("click", () => {
    moNewModal.classList.remove("open");
    moNewModal.setAttribute("aria-hidden", "true");
  });
  moNewModal.addEventListener("click", (e) => {
    if (e.target === moNewModal) {
      moNewModal.classList.remove("open");
      moNewModal.setAttribute("aria-hidden", "true");
    }
  });

  document.getElementById("mo-new-save").addEventListener("click", async () => {
    const msg = document.getElementById("mo-new-msg");
    msg.textContent = "";
    const supplier = document.getElementById("mo-new-sup").value.trim();
    const materialId = Number(document.getElementById("mo-new-mat").value);
    const quantity = Number(document.getElementById("mo-new-qty").value);
    if (!supplier || !materialId || Number.isNaN(quantity) || quantity < 1) {
      msg.textContent = "Укажите поставщика, сырьё и количество.";
      return;
    }
    try {
      await apiJson("materialorders", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          supplier,
          materialId,
          quantity,
          staffId: user.staffId,
          status: "Ожидает поставки",
        }),
      });
      moNewModal.classList.remove("open");
      moNewModal.setAttribute("aria-hidden", "true");
      await loadMaterials();
      await loadOrders();
    } catch (e) {
      msg.textContent = e.message || "Ошибка оформления заказа";
    }
  });

  const moEdModal = document.getElementById("mo-modal-edit");
  function closeEditMo() {
    moEdModal.classList.remove("open");
    moEdModal.setAttribute("aria-hidden", "true");
    document.getElementById("mo-ed-msg").textContent = "";
  }

  function openEditOrderModal(o) {
    document.getElementById("mo-ed-msg").textContent = "";
    document.getElementById("mo-ed-id").value = String(o.materialOrderId);
    document.getElementById("mo-ed-staff").value = String(o.staffId);
    document.getElementById("mo-ed-sup").value = o.supplier || "";
    fillMaterialSelect(document.getElementById("mo-ed-mat"));
    document.getElementById("mo-ed-mat").value = String(o.materialId);
    document.getElementById("mo-ed-qty").value = String(o.quantity);
    document.getElementById("mo-ed-status").value = o.status || "Ожидает поставки";
    moEdModal.classList.add("open");
    moEdModal.setAttribute("aria-hidden", "false");
  }

  document.getElementById("mo-ed-cancel").addEventListener("click", closeEditMo);
  moEdModal.addEventListener("click", (e) => {
    if (e.target === moEdModal) closeEditMo();
  });

  document.getElementById("mo-ed-save").addEventListener("click", async () => {
    const msg = document.getElementById("mo-ed-msg");
    msg.textContent = "";
    const id = Number(document.getElementById("mo-ed-id").value);
    const staffId = Number(document.getElementById("mo-ed-staff").value);
    const supplier = document.getElementById("mo-ed-sup").value.trim();
    const materialId = Number(document.getElementById("mo-ed-mat").value);
    const quantity = Number(document.getElementById("mo-ed-qty").value);
    const status = document.getElementById("mo-ed-status").value;
    if (!supplier || !materialId || Number.isNaN(quantity) || quantity < 1) {
      msg.textContent = "Проверьте поля заказа.";
      return;
    }
    try {
      await apiJson(`materialorders/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          materialOrderId: id,
          supplier,
          materialId,
          quantity,
          staffId,
          status,
        }),
      });
      closeEditMo();
      await loadMaterials();
      await loadOrders();
    } catch (e) {
      msg.textContent = e.message || "Ошибка сохранения";
    }
  });

  // Вкладка "Анализ потребности в материалах"

  const ndBody = document.getElementById("nd-body");
  const ndUsageBody = document.getElementById("nd-usage-body");
  const ndMsg = document.getElementById("nd-msg");

  document.getElementById("nd-run").addEventListener("click", async () => {
    ndMsg.textContent = "";
    tbodyClear(ndBody);
    tbodyClear(ndUsageBody);
    if (typeof destroyMaterialCharts === "function") {
      destroyMaterialCharts();
    }
    const ndNeedsWrap = document.getElementById("nd-charts-needs-wrap");
    const ndUsageWrap = document.getElementById("nd-charts-usage-wrap");
    if (ndNeedsWrap) ndNeedsWrap.style.display = "none";
    if (ndUsageWrap) ndUsageWrap.style.display = "none";

    const from = document.getElementById("nd-from").value;
    const to = document.getElementById("nd-to").value;
    const qs = [];
    if (from) qs.push(`from=${encodeURIComponent(from)}`);
    if (to) qs.push(`to=${encodeURIComponent(to)}`);
    const path =
      qs.length > 0
        ? `reports/material-needs?${qs.join("&")}`
        : "reports/material-needs";

    try {
      const needs = await apiJson(path, { method: "GET" });
      needs.forEach((r) => {
        const tr = document.createElement("tr");
        [
          r.materialName,
          r.unit,
          r.quantityAvailable,
          r.requiredQuantity,
          r.deficit,
        ].forEach((text) => {
          const td = document.createElement("td");
          td.textContent = text == null ? "—" : String(text);
          tr.appendChild(td);
        });
        ndBody.appendChild(tr);
      });

      const usage = await apiJson("reports/material-usage", { method: "GET" });
      usage.forEach((r) => {
        const tr = document.createElement("tr");
        [r.materialName, r.unit, r.totalRequired].forEach((text) => {
          const td = document.createElement("td");
          td.textContent = text == null ? "—" : String(text);
          tr.appendChild(td);
        });
        ndUsageBody.appendChild(tr);
      });

      if (typeof renderMaterialNeedsPie === "function") {
        if (ndNeedsWrap) ndNeedsWrap.style.display = "grid";
        renderMaterialNeedsPie(
          document.getElementById("chart-nd-needs"),
          needs
        );
      }
      if (typeof renderMaterialUsagePie === "function") {
        if (ndUsageWrap) ndUsageWrap.style.display = "grid";
        renderMaterialUsagePie(
          document.getElementById("chart-nd-usage"),
          usage
        );
      }
    } catch (e) {
      ndMsg.className = "msg error";
      ndMsg.textContent = e.message || "Ошибка расчёта";
    }
  });

  Promise.all([loadMaterials(), loadOrders()]).catch((e) => {
    alert(
      "Ошибка загрузки: " + (e.message || e) + ". Проверьте API."
    );
  });
})();
