const apiUrl = "https://localhost:7133/api";
let registeredEmail = "";

document.addEventListener("DOMContentLoaded", () => {
    const savedTheme = localStorage.getItem('theme');
    const themeIcon = document.getElementById('themeIcon');
    const html = document.documentElement;

    if (savedTheme === 'light') {
        html.classList.remove('dark');
        if (themeIcon) {
            themeIcon.textContent = '☀️';
            themeIcon.style.color = '#f59e0b';
        }
    } else {
        html.classList.add('dark');
        if (themeIcon) {
            themeIcon.textContent = '🌙';
            themeIcon.style.color = '#fbbf24';
        }
    }
});

function toggleTheme() {
    const html = document.documentElement;
    const themeIcon = document.getElementById('themeIcon');

    if (html.classList.contains('dark')) {
        html.classList.remove('dark');
        if (themeIcon) {
            themeIcon.textContent = '☀️';
            themeIcon.style.color = '#f59e0b';
        }
        localStorage.setItem('theme', 'light');
    } else {
        html.classList.add('dark');
        if (themeIcon) {
            themeIcon.textContent = '🌙';
            themeIcon.style.color = '#fbbf24';
        }
        localStorage.setItem('theme', 'dark');
    }

    renderGoogleButton();
}

async function registerUser() {
    const usernameInput = document.getElementById("reg-username");
    const emailInput = document.getElementById("reg-email");
    const passwordInput = document.getElementById("reg-password");

    if (!usernameInput.value.trim() || !emailInput.value.trim() || !passwordInput.value.trim()) {
        alert("Lütfen tüm alanları doldurunuz.");
        return;
    }

    const userData = {
        Username: usernameInput.value,
        Email: emailInput.value,
        Password: passwordInput.value
    };

    try {
        const response = await fetch(`${apiUrl}/Auth/register`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(userData)
        });

        if (response.ok) {
            registeredEmail = emailInput.value;
            alert("✅ Kayıt Başarılı! Lütfen e-postanıza gelen 6 haneli doğrulama kodunu girin.");

            const regForm = document.getElementById("registerContainer");
            const verifyForm = document.getElementById("verifyContainer");
            if (regForm && verifyForm) {
                regForm.classList.add("hidden");
                verifyForm.classList.remove("hidden");
            }
        } else {
            const errorText = await response.text();
            alert("❌ Hata: " + errorText);
        }
    } catch (error) {
        alert("Sunucuya bağlanılamadı!");
    }
}

async function verifyCode() {
    const codeInput = document.getElementById("verify-code");

    if (!codeInput || !codeInput.value.trim() || codeInput.value.length !== 6) {
        alert("Lütfen 6 haneli doğrulama kodunu girin.");
        return;
    }

    const verifyData = {
        Email: registeredEmail,
        Code: codeInput.value.trim(),
        Username: "dummy",
        Password: "dummy"
    };

    try {
        const response = await fetch(`${apiUrl}/Auth/verify`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(verifyData)
        });

        if (response.ok) {
            alert("✅ E-posta doğrulandı! Giriş sayfasına yönlendiriliyorsunuz...");
            window.location.href = "login.html";
        } else {
            const errorText = await response.text();
            alert("❌ Hata: " + errorText);
        }
    } catch (error) {
        alert("Sunucuya bağlanılamadı!");
    }
}

async function loginUser() {
    const usernameInput = document.getElementById("login-username");
    const passwordInput = document.getElementById("login-password");

    if (!usernameInput.value.trim() || !passwordInput.value.trim()) {
        alert("Lütfen Alanları Doldurunuz.");
        return;
    }

    const loginData = {
        Username: usernameInput.value,
        Password: passwordInput.value,
        Email: "dummy@dummy.com"
    };

    try {
        const response = await fetch(`${apiUrl}/Auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(loginData)
        });

        if (response.ok) {
            const data = await response.json();

            if (data.token) {
                localStorage.setItem("jwtToken", data.token);
                localStorage.setItem("username", data.username || usernameInput.value);

                alert("Giriş Başarılı! Yönlendiriliyorsunuz...");
                window.location.href = "index.html";
            } else {
                alert("Sistemsel bir hata oluştu: Token alınamadı.");
            }
        } else {
            const errorMsg = await response.text();
            alert("❌ Giriş Başarısız: " + errorMsg);
        }
    } catch (error) {
        alert("Sunucuya bağlanılamadı!");
    }
}

window.onload = function () {
    if (document.getElementById("googleButtonContainer")) {
        const isRegisterPage = window.location.href.includes('register');

        google.accounts.id.initialize({
            client_id: "787940789409-k70mn4qf4fatqgsjnlr3h7fn8dj7bklt.apps.googleusercontent.com",
            callback: isRegisterPage ? handleGoogleRegister : handleGoogleLogin
        });

        renderGoogleButton();

        window.addEventListener('resize', () => {
            clearTimeout(window.resizeTimer);
            window.resizeTimer = setTimeout(renderGoogleButton, 200);
        });
    }
};

function renderGoogleButton() {
    const container = document.getElementById("googleButtonContainer");
    if (container && window.google) {
        container.innerHTML = "";

        const isDarkMode = document.documentElement.classList.contains('dark');
        const btnWidth = container.offsetWidth || 340;

        google.accounts.id.renderButton(
            container,
            { theme: isDarkMode ? "filled_black" : "outline", size: "large", width: btnWidth, text: "continue_with" }
        );
    }
}

async function handleGoogleLogin(response) {
    try {
        const res = await fetch(`${apiUrl}/Auth/google-login`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ Token: response.credential })
        });

        if (res.ok) {
            const data = await res.json();
            localStorage.setItem("jwtToken", data.token);
            localStorage.setItem("username", data.username);
            window.location.href = "index.html";
        } else {
            const err = await res.text();
            alert("❌ " + err);
        }
    } catch (error) {
        alert("Bağlantı hatası oluştu.");
    }
}

async function handleGoogleRegister(response) {
    try {
        const res = await fetch(`${apiUrl}/Auth/google-register`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ Token: response.credential })
        });

        if (res.ok) {
            const data = await res.json();
            localStorage.setItem("jwtToken", data.token);
            localStorage.setItem("username", data.username);
            window.location.href = "index.html";
        } else {
            const err = await res.text();
            alert("❌ " + err);
        }
    } catch (error) {
        alert("Bağlantı hatası oluştu.");
    }
}

function loginWithMicrosoft() {
    window.location.href = `${apiUrl}/Auth/microsoft-login`;
}

function registerWithMicrosoft() {
    window.location.href = `${apiUrl}/Auth/microsoft-register`;
}