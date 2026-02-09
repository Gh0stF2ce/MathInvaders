let currentUser = null;

window.onload = function () {
    console.log("Page loaded, checking for currentUser in localStorage...");
    const user = localStorage.getItem('currentUser');
    if (user) {
        currentUser = JSON.parse(user);
        document.getElementById('authSection').style.display = 'none';
        document.getElementById('userSection').style.display = 'flex';
        document.getElementById('userName').textContent = `Привет, ${currentUser.name}!`;
        console.log("User loaded:", currentUser);
    } else {
        console.log("No user found in localStorage");
    }
};

function logout() {
    console.log("logout called");
    currentUser = null;
    localStorage.removeItem('currentUser');
    window.location.href = '/Home/Index';
}

$(document).ready(function () {
    $("#registerForm").validate({
        rules: {
            Name: { required: true, minlength: 2, maxlength: 50 },
            Email: { required: true, email: true },
            Password: {
                required: true,
                minlength: 8,
                maxlength: 100,
                regex: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$/,
                noSpaces: true
            },
            ConfirmPassword: { required: true, equalTo: "#Password" }
        },
        messages: {
            Name: {
                required: "Имя обязательно",
                minlength: "Имя должно содержать минимум 2 символа",
                maxlength: "Имя не должно превышать 50 символов"
            },
            Email: {
                required: "Email обязателен",
                email: "Введите корректный email"
            },
            Password: {
                required: "Пароль обязателен",
                minlength: "Пароль должен содержать минимум 8 символов",
                maxlength: "Пароль не должен превышать 100 символов",
                regex: "Пароль должен содержать заглавную букву, строчную букву, цифру и специальный символ (@$!%*?&)",
                noSpaces: "Пароль не должен содержать пробелы"
            },
            ConfirmPassword: {
                required: "Подтверждение пароля обязательно",
                equalTo: "Пароли должны совпадать"
            }
        },
        errorElement: "span",
        errorClass: "field-validation-error"
    });

    $.validator.addMethod("noSpaces", function (value, element) {
        return this.optional(element) || !/\s/.test(value);
    }, "Пароль не должен содержать пробелы");

    $.validator.addMethod("regex", function (value, element, regexp) {
        var re = new RegExp(regexp);
        return this.optional(element) || re.test(value);
    }, "Пароль должен соответствовать требованиям");
});