// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.turniej-group').forEach(function (tbody) {
        // avoid initializing twice
        if (tbody.dataset.toggleAttached === '1') return;

        var rows = Array.from(tbody.querySelectorAll('tr'));
        if (rows.length > 1) {
            // hide all but first
            rows.slice(1).forEach(r => r.style.display = 'none');
            // add toggle on first row if not already present
            var first = rows[0];
            var existingBtn = first.querySelector('.toggle-turniej');
            if (!existingBtn) {
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'btn btn-sm btn-link p-0 ms-2 toggle-turniej';
                btn.innerHTML = '<i class="fas fa-chevron-right"></i>';
                btn.setAttribute('aria-expanded', 'false');

                btn.addEventListener('click', function () {
                    var hidden = rows.slice(1).some(r => r.style.display === 'none');
                    if (hidden) {
                        rows.slice(1).forEach(r => r.style.display = 'table-row');
                        btn.innerHTML = '<i class="fas fa-chevron-down"></i>';
                        tbody.classList.add('turniej-expanded');
                        btn.setAttribute('aria-expanded', 'true');
                    } else {
                        rows.slice(1).forEach(r => r.style.display = 'none');
                        btn.innerHTML = '<i class="fas fa-chevron-right"></i>';
                        tbody.classList.remove('turniej-expanded');
                        btn.setAttribute('aria-expanded', 'false');
                    }
                });

                // append toggle to first cell of first row after the match number
                var cell = first.querySelector('td');
                if (cell) cell.appendChild(btn);
            }
        }

        // mark as initialized to avoid duplicate attachments
        tbody.dataset.toggleAttached = '1';
    });
});
