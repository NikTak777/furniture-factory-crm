(function () {
  function apply() {
    if (typeof Chart === "undefined") return;
    Chart.defaults.color = "#94a3b8";
    Chart.defaults.borderColor = "rgba(148, 163, 184, 0.25)";
    Chart.defaults.plugins.title.color = "#e2e8f0";
    Chart.defaults.plugins.legend.labels.color = "#94a3b8";
    Chart.defaults.plugins.tooltip.backgroundColor = "rgba(30, 41, 59, 0.96)";
    Chart.defaults.plugins.tooltip.titleColor = "#f8fafc";
    Chart.defaults.plugins.tooltip.bodyColor = "#cbd5e1";
    Chart.defaults.plugins.tooltip.borderColor = "rgba(148, 163, 184, 0.35)";
    Chart.defaults.plugins.tooltip.padding = 10;
  }
  apply();
  document.addEventListener("DOMContentLoaded", apply);
})();
