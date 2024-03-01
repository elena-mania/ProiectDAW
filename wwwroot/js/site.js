// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.querySelectorAll('.articles__article').forEach(function (card) {
    card.addEventListener('click', function () {
        window.location.href = this.getAttribute('data-url');
    });
});
