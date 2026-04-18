/**
 * Диаграммы отчёта о производстве:
 * круговая — доля заказов по статусам; столбчатая — топ‑5 товаров по выручке среди выполненных.
 */
(function (global) {
  const PALETTE = [
    "rgb(91, 155, 213)",
    "rgb(237, 125, 49)",
    "rgb(133, 83, 166)",
    "rgb(255, 192, 0)",
    "rgb(84, 130, 53)",
    "rgb(68, 114, 196)",
    "rgb(165, 165, 165)",
    "rgb(192, 128, 64)",
    "rgb(255, 153, 204)",
    "rgb(146, 208, 80)",
  ];

  let pieChart = null;
  let barChart = null;

  function destroyCharts() {
    if (pieChart) {
      pieChart.destroy();
      pieChart = null;
    }
    if (barChart) {
      barChart.destroy();
      barChart = null;
    }
  }

  function statusSliceColor(status, paletteIndex) {
    const s = (status || "").trim();
    if (/выполн/i.test(s)) return "rgb(67, 160, 71)";
    if (/отмен/i.test(s)) return "rgb(229, 57, 53)";
    return PALETTE[paletteIndex % PALETTE.length];
  }

  /**
   * @param {string} wrapId - id контейнера (.chart-row), показывается если есть данные
   * @param {Array<{ status?: string; productName?: string; totalPrice?: number }>} orders
   */
  global.renderProductionReportCharts = function (wrapId, orders) {
    destroyCharts();
    const wrap = document.getElementById(wrapId);
    if (!wrap) return;

    const pieCanvas = wrap.querySelector("[data-chart-role='prod-status']");
    const barCanvas = wrap.querySelector("[data-chart-role='prod-products']");
    if (!pieCanvas || !barCanvas) return;

    if (!orders || orders.length === 0) {
      wrap.style.display = "none";
      return;
    }

    wrap.style.display = "grid";

    const statusMap = {};
    orders.forEach((o) => {
      const k = o.status || "—";
      statusMap[k] = (statusMap[k] || 0) + 1;
    });
    const labels = Object.keys(statusMap);
    const counts = labels.map((l) => statusMap[l]);
    const colors = labels.map((l, i) => statusSliceColor(l, i));

    pieChart = new Chart(pieCanvas.getContext("2d"), {
      type: "doughnut",
      data: {
        labels,
        datasets: [
          {
            data: counts,
            backgroundColor: colors,
            borderColor: "rgba(15, 23, 42, 0.9)",
            borderWidth: 1,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: "bottom",
            labels: { boxWidth: 12, padding: 10 },
          },
          title: {
            display: true,
            text: "Заказы по статусам",
            font: { size: 14 },
          },
        },
      },
    });

    const completed = orders.filter((o) => {
      const st = o.status || "";
      return (
        st === "Выполнено" ||
        st === "Выполнен" ||
        /выполн/i.test(st)
      );
    });
    const revByProduct = {};
    completed.forEach((o) => {
      const name = o.productName || "—";
      revByProduct[name] =
        (revByProduct[name] || 0) + (Number(o.totalPrice) || 0);
    });
    const sorted = Object.entries(revByProduct)
      .sort((a, b) => b[1] - a[1])
      .slice(0, 5);

    if (sorted.length === 0) {
      barChart = new Chart(barCanvas.getContext("2d"), {
        type: "bar",
        data: {
          labels: ["Нет данных"],
          datasets: [
            {
              label: "Выручка",
              data: [0],
              backgroundColor: "rgba(148, 163, 184, 0.3)",
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false },
            title: {
              display: true,
              text: "Топ товаров по выручке (выполненные заказы)",
              font: { size: 14 },
            },
          },
          scales: {
            y: { beginAtZero: true, ticks: { color: "#94a3b8" } },
            x: { ticks: { color: "#94a3b8", maxRotation: 45 } },
          },
        },
      });
      return;
    }

    const barLabels = sorted.map(([name]) =>
      name.length > 26 ? `${name.slice(0, 24)}…` : name
    );
    const barData = sorted.map(([, v]) => v);
    const barRgb = sorted.map((_, i) => PALETTE[i % PALETTE.length]);

    barChart = new Chart(barCanvas.getContext("2d"), {
      type: "bar",
      data: {
        labels: barLabels,
        datasets: [
          {
            label: "Выручка, ₽",
            data: barData,
            backgroundColor: barRgb.map((rgb) => {
              const m = rgb.match(/rgb\((\d+),\s*(\d+),\s*(\d+)\)/);
              return m
                ? `rgba(${m[1]},${m[2]},${m[3]},0.78)`
                : rgb;
            }),
            borderColor: barRgb,
            borderWidth: 1,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          title: {
            display: true,
            text: "Топ‑5 товаров по выручке (выполненные)",
            font: { size: 14 },
          },
        },
        scales: {
          y: { beginAtZero: true, ticks: { color: "#94a3b8" } },
          x: { ticks: { color: "#94a3b8", maxRotation: 40 } },
        },
      },
    });
  };

  global.destroyProductionReportCharts = destroyCharts;
})(typeof window !== "undefined" ? window : globalThis);
