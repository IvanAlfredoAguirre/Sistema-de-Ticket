// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification

// resalta link activo si usás query 'f' en filtros
const params = new URLSearchParams(window.location.search);
const f = params.get('f');
if (f) {
    document.querySelectorAll('.filters a').forEach(a => {
        if (a.href.includes('f=' + f)) a.classList.add('btn-secondary');
    });
}
