// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    const navLinks = document.querySelectorAll("#shefaNavPills .nav-link");

    // 1. Move active pill immediately on click
    navLinks.forEach(link => {
        link.addEventListener("click", function () {
            navLinks.forEach(item => item.classList.remove("active"));
            this.classList.add("active");
        });
    });

    // 2. Highlight active pill based on URL hash on initial page load (e.g., coming back from /Doctors#specialties)
    if (window.location.hash) {
        const targetSection = window.location.hash.replace("#", "");
        const matchingLink = document.querySelector(`#shefaNavPills .nav-link[data-section="${targetSection}"]`);
        if (matchingLink) {
            navLinks.forEach(item => item.classList.remove("active"));
            matchingLink.classList.add("active");
        }
    }

    // 3. Scroll-spy: Automatically switch active pill as user scrolls sections on the Home page
    const sectionIds = ["top-doctors", "specialties", "how-it-works", "about", "join-as-doctor"];
    window.addEventListener("scroll", function () {
        // Only run scroll-spy if we are on the Home page (where the "home" nav item exists and sections exist)
        const homeLink = document.querySelector('#shefaNavPills .nav-link[data-section="home"]');
        if (!homeLink || !document.getElementById("specialties")) return;

        let currentSection = "home";
        const scrollPosition = window.scrollY + 180; // Offset for sticky navbar

        sectionIds.forEach(id => {
            const el = document.getElementById(id);
            if (el && el.offsetTop <= scrollPosition) {
                currentSection = id;
            }
        });

        navLinks.forEach(link => {
            link.classList.toggle("active", link.getAttribute("data-section") === currentSection);
        });
    });
});