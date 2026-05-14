(function () {
  /**
   * Редактируемый комбобокс: одно поле ввода + выпадающий список.
   * @param {{
   *   container: HTMLElement;
   *   inputId?: string;
   *   placeholder?: string;
   *   emptyHint?: string;
   *   getOptions: (query: string) => Array<{ id: string | number; label: string }>;
   *   onChange?: () => void;
   * }} config
   */
  window.createSearchCombobox = function (config) {
    const {
      container,
      inputId,
      placeholder = "",
      emptyHint = "Ничего не найдено",
      getOptions,
      onChange,
    } = config;

    container.classList.add("combobox");

    const input = document.createElement("input");
    input.type = "text";
    input.autocomplete = "off";
    input.className = "combobox-input";
    if (inputId) input.id = inputId;
    input.placeholder = placeholder;
    input.setAttribute("role", "combobox");
    input.setAttribute("aria-autocomplete", "list");
    input.setAttribute("aria-expanded", "false");

    const list = document.createElement("ul");
    list.className = "combobox-list";
    list.setAttribute("role", "listbox");
    list.hidden = true;

    container.appendChild(input);
    container.appendChild(list);

    /** @type {string | number | null} */
    let selectedId = null;
    let activeIndex = -1;
    let suppressInput = false;
    /** @type {Map<string, string>} */
    const pinnedOptions = new Map();

    function visibleOptions() {
      const q = input.value;
      const items = getOptions(q);
      const seen = new Set();
      const out = [];

      function push(item) {
        const key = String(item.id);
        if (seen.has(key)) return;
        seen.add(key);
        out.push({ id: item.id, label: item.label });
      }

      if (selectedId != null) {
        const key = String(selectedId);
        const pinned = pinnedOptions.get(key);
        if (pinned && (!q || pinned.toLowerCase().includes(q.trim().toLowerCase()))) {
          push({ id: selectedId, label: pinned });
        }
      }

      items.forEach(push);
      return out;
    }

    function renderList() {
      const items = visibleOptions();
      list.innerHTML = "";
      if (!items.length) {
        const li = document.createElement("li");
        li.className = "combobox-empty";
        li.textContent = emptyHint;
        list.appendChild(li);
        activeIndex = -1;
        return;
      }
      if (activeIndex >= items.length) activeIndex = items.length - 1;
      items.forEach((item, idx) => {
        const li = document.createElement("li");
        li.className = "combobox-option";
        li.setAttribute("role", "option");
        li.dataset.id = String(item.id);
        li.textContent = item.label;
        if (idx === activeIndex) {
          li.classList.add("is-active");
          li.setAttribute("aria-selected", "true");
        }
        li.addEventListener("mousedown", (e) => {
          e.preventDefault();
          selectOption(item);
        });
        list.appendChild(li);
      });
    }

    function open() {
      if (input.disabled || input.readOnly) return;
      list.hidden = false;
      input.setAttribute("aria-expanded", "true");
      container.classList.add("is-open");
      renderList();
    }

    function close() {
      list.hidden = true;
      activeIndex = -1;
      input.setAttribute("aria-expanded", "false");
      container.classList.remove("is-open");
    }

    function selectOption(item) {
      suppressInput = true;
      selectedId = item.id;
      input.value = item.label;
      pinnedOptions.set(String(item.id), item.label);
      suppressInput = false;
      close();
      if (onChange) onChange();
    }

    function clear() {
      selectedId = null;
      input.value = "";
      pinnedOptions.clear();
      close();
    }

    function setValue(id, label) {
      selectedId = id;
      suppressInput = true;
      input.value = label || "";
      suppressInput = false;
      if (id != null && label) pinnedOptions.set(String(id), label);
      close();
    }

    function getValue() {
      return selectedId;
    }

    function setDisabled(disabled) {
      input.disabled = disabled;
      if (disabled) close();
    }

    function setReadOnly(readonly) {
      input.readOnly = readonly;
      input.classList.toggle("combobox-input-readonly", readonly);
      if (readonly) close();
    }

    input.addEventListener("focus", () => {
      if (!input.disabled && !input.readOnly) open();
    });

    input.addEventListener("input", () => {
      if (suppressInput) return;
      selectedId = null;
      activeIndex = -1;
      open();
      renderList();
      if (onChange) onChange();
    });

    input.addEventListener("keydown", (e) => {
      if (input.disabled || input.readOnly) return;
      const items = visibleOptions();
      if (e.key === "ArrowDown") {
        e.preventDefault();
        if (list.hidden) open();
        if (!items.length) return;
        activeIndex = activeIndex < items.length - 1 ? activeIndex + 1 : 0;
        renderList();
        const active = list.querySelector(".combobox-option.is-active");
        if (active) active.scrollIntoView({ block: "nearest" });
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        if (list.hidden) open();
        if (!items.length) return;
        activeIndex = activeIndex > 0 ? activeIndex - 1 : items.length - 1;
        renderList();
        const active = list.querySelector(".combobox-option.is-active");
        if (active) active.scrollIntoView({ block: "nearest" });
      } else if (e.key === "Enter") {
        if (!list.hidden && activeIndex >= 0 && items[activeIndex]) {
          e.preventDefault();
          selectOption(items[activeIndex]);
        }
      } else if (e.key === "Escape") {
        close();
      }
    });

    input.addEventListener("blur", () => {
      window.setTimeout(() => {
        if (!container.contains(document.activeElement)) close();
      }, 120);
    });

    document.addEventListener("mousedown", (e) => {
      if (!container.contains(e.target)) close();
    });

    return {
      input,
      clear,
      setValue,
      getValue,
      getText: () => input.value.trim(),
      setDisabled,
      setReadOnly,
      focus: () => input.focus(),
      close,
    };
  };
})();
