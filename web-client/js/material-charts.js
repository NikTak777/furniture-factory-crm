/**
 * Диаграммы анализа потребности в материалах
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

  let needsChart = null;
  let usageChart = null;

  function destroyCharts() {
    if (needsChart) {
      needsChart.destroy();
      needsChart = null;
    }
    if (usageChart) {
      usageChart.destroy();
      usageChart = null;
    }
  }

  /**
   * Потребность: доли requiredQuantity, слив <3% в «Другое»
   */
  global.renderMaterialNeedsPie = function (canvas, needsItems) {
    if (needsChart) {
      needsChart.destroy();
      needsChart = null;
    }
    if (!canvas) return;

    const positive = (needsItems || []).filter(
      (x) => (x.requiredQuantity || 0) > 0
    );
    const total = positive.reduce((s, x) => s + x.requiredQuantity, 0);
    if (total <= 0) {
      needsChart = new Chart(canvas.getContext("2d"), {
        type: "pie",
        data: {
          labels: ["Нет данных"],
          datasets: [
            {
              data: [1],
              backgroundColor: ["rgba(148, 163, 184, 0.35)"],
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            title: {
              display: true,
              text: "Структура потребности по материалам",
              font: { size: 14 },
            },
            legend: { position: "bottom" },
          },
        },
      });
      return;
    }

    const threshold = 0.03;
    const labels = [];
    const values = [];
    const colors = [];
    let other = 0;
    let idx = 0;

    positive.forEach((item) => {
      const frac = item.requiredQuantity / total;
      if (frac < threshold) {
        other += item.requiredQuantity;
      } else {
        labels.push(item.materialName || `#${item.materialId}`);
        values.push(item.requiredQuantity);
        colors.push(PALETTE[idx % PALETTE.length]);
        idx++;
      }
    });
    if (other > 0) {
      labels.push("Другое");
      values.push(other);
      colors.push(PALETTE[idx % PALETTE.length]);
    }

    needsChart = new Chart(canvas.getContext("2d"), {
      type: "pie",
      data: {
        labels,
        datasets: [
          {
            data: values,
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
          title: {
            display: true,
            text: "Структура потребности по материалам",
            font: { size: 14 },
          },
          legend: {
            position: "bottom",
            labels: { boxWidth: 10, font: { size: 11 } },
          },
        },
      },
    });
  };

  /**
   * Топ‑9 материалов по потреблению за 3 мес., остальное — «Другое»
   */
  global.renderMaterialUsagePie = function (canvas, usageItems) {
    if (usageChart) {
      usageChart.destroy();
      usageChart = null;
    }
    if (!canvas) return;

    const filtered = (usageItems || []).filter(
      (x) => (x.totalRequired || 0) > 0
    );
    filtered.sort((a, b) => b.totalRequired - a.totalRequired);

    if (filtered.length === 0) {
      usageChart = new Chart(canvas.getContext("2d"), {
        type: "pie",
        data: {
          labels: ["Нет данных"],
          datasets: [
            {
              data: [1],
              backgroundColor: ["rgba(148, 163, 184, 0.35)"],
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            title: {
              display: true,
              text: "Потребление материалов (3 мес.)",
              font: { size: 14 },
            },
            legend: { position: "bottom" },
          },
        },
      });
      return;
    }

    const labels = [];
    const values = [];
    const colors = [];
    let otherTotal = 0;
    let colorIdx = 0;

    filtered.forEach((u, i) => {
      if (i < 9) {
        labels.push(u.materialName || `#${u.materialId}`);
        values.push(u.totalRequired);
        colors.push(PALETTE[colorIdx % PALETTE.length]);
        colorIdx++;
      } else {
        otherTotal += u.totalRequired;
      }
    });
    if (otherTotal > 0) {
      labels.push("Другое");
      values.push(otherTotal);
      colors.push(PALETTE[colorIdx % PALETTE.length]);
    }

    usageChart = new Chart(canvas.getContext("2d"), {
      type: "pie",
      data: {
        labels,
        datasets: [
          {
            data: values,
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
          title: {
            display: true,
            text: "Потребление материалов (топ‑9 и прочее)",
            font: { size: 14 },
          },
          legend: {
            position: "bottom",
            labels: { boxWidth: 10, font: { size: 11 } },
          },
        },
      },
    });
  };

  global.destroyMaterialCharts = destroyCharts;
})(typeof window !== "undefined" ? window : globalThis);
