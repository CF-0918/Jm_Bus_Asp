$(document).ready(function () {
    // Define the toggleSidebar function

    // Show the overlay and review form when the notification is clicked
    $('.review_circle_notification').on('click', function () {
        $('.review_overlay').fadeIn();
        $('.review_container').fadeIn();
    });

    // Hide the overlay and review form when the close button is clicked
    $('.review_close_btn').on('click', function () {
        $('.review_overlay').fadeOut();
        $('.review_container').fadeOut();
    });

    // Enable/disable the comment box based on rating selection
    $('.rate_review').on('change', function () {
        var reviewValue = $(this).data('review-value'); // Get the review value from the data attribute
        var commentBox = $('#bad_review_comment_box'); // The container for the comment box
        var comment = $('#review_comment'); // The textarea inside the comment box

        // Check if the rating value is less than or equal to 4
        if (parseInt(reviewValue) <= 2) {
            // Show the comment box and enable the textarea
            commentBox.show();
            comment.prop('disabled', false); // Enable the textarea
        } else {
            // Hide the comment box and disable the textarea
            commentBox.hide();
            comment.prop('disabled', true); // Disable the textarea
        }
    });

    $("#autoGeneratePassword").on('click', autoGeneratePassword);

    function autoGeneratePassword() {
        var length = 10;
        var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_+*&%";
        var password = "";
        for (var i = 0; i < length; i++) {
            password += characters.charAt(Math.floor(Math.random() * characters.length));
        }
        $("#password").val(password);
        checkRegisterForm();
    }

    $(".eye_button").on('click', togglePasswords);

    function togglePasswords() {
        var passwordInput = $("#password");
        var eyeIcon = $(".eye_button i");
        if (passwordInput.attr("type") === "password") {
            passwordInput.attr("type", "text");
            eyeIcon.removeClass("fa-eye");
            eyeIcon.addClass("fa-eye-slash");
        } else {
            passwordInput.attr("type", "password");
            eyeIcon.removeClass("fa-eye-slash");
            eyeIcon.addClass("fa-eye");
        }

    }

    $(".createAcc").on('click', checkRegisterForm);

    // Initially disable the submit button
    $("#createAcc").prop("disabled", true);

    // Function to validate all fields before enabling submit
    function checkRegisterForm() {
        var isValid = true;
        var errors = {};
        var fields = ["name", "email", "position", "password"];

        // Validate each field
        var name = $("#name").val().trim();
        if (name === "") {
            isValid = false;
            errors["name"] = ["Name is required"];
        }

        var email = $("#email").val().trim();
        if (email === "") {
            isValid = false;
            errors["email"] = ["Email is required"];
        } else if (!validateEmail(email)) {
            isValid = false;
            errors["email"] = ["Invalid email format"];
        }

        var position = $("#position").val().trim();
        if (position === "") {
            isValid = false;
            errors["position"] = ["Position is required"];
        }

        var password = $("#password").val();
        if (password === "") {
            isValid = false;
            errors["password"] = ["Password is required"];
        } else if (password.length < 8) {
            isValid = false;
            errors["password"] = errors["password"] || [];
            errors["password"].push("Password must be at least 8 characters long");
        }

        // Display errors for each field
        fields.forEach(field => {
            var errorText = errors[field] ? errors[field].join(", ") : "";
            $("#" + field + "Error").html(errorText);
        });

        // Enable or disable the submit button based on the form's validity
        $("#createAcc").prop("disabled", !isValid);
    }

    // Example email validation function
    function validateEmail(email) {
        var regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return regex.test(email);
    }

    // Attach the checkRegisterForm function to the input events
    $("#name, #email, #position, #password").on("input", function () {
        checkRegisterForm();
    });


    // Submit form only if validation passes
    $("#registerForm").on("submit", function (event) {
        event.preventDefault();
        checkRegisterForm();

        // Check one last time if form is valid before submitting
        if ($("#createAcc").is(":disabled")) {
            alert("Please fill out all required fields correctly.");
        } else {
            this.submit(); // Submit the form if no errors
        }
    });
    $('#checkAll').click(function () {
        $('.checkBox').prop('checked', this.checked);
    });

    // Optionally, allow "checkAll" to uncheck if any checkbox is unchecked
    $('.checkBox').click(function () {
        $('#checkAll').prop('checked', $('.checkBox:checked').length === $('.checkBox').length);
    });

    $('[data-upper]').on('input', function (e) {
        // Get the current value
        const value = e.target.value;

        // Remove any non-alphabetic characters except spaces and convert to uppercase
        const sanitizedValue = value.replace(/[^A-Za-z\s]/g, '').toUpperCase();

        // Get the cursor positions before updating the value
        const a = e.target.selectionStart;
        const b = e.target.selectionEnd;

        // Update the input field with the sanitized value
        e.target.value = sanitizedValue;

        // Restore the cursor positions
        e.target.setSelectionRange(a, b);
    });



    $('[data-digit]').on('input', function (e) {
        // Get the current value
        const value = e.target.value;

        // Remove any non-digit characters
        const sanitizedValue = value.replace(/\D/g, '');

        // Update the input field with the sanitized value
        e.target.value = sanitizedValue;
    });

    $('[data-digit]').on('input', function (e) {
        // Get the current value
        const value = e.target.value;

        // Remove any non-digit characters
        const sanitizedValue = value.replace(/\D/g, '');

        // Update the input field with the sanitized value
        e.target.value = sanitizedValue;
    });

    $('[data-icNo]').on('input', function (e) {
        // Get the current value
        const value = e.target.value.replace(/\D/g, ''); // Remove all non-digit characters

        // Format the value as `XXXXXX-XX-XXXX`
        let formattedValue = '';
        if (value.length <= 6) {
            formattedValue = value;
        } else if (value.length <= 8) {
            formattedValue = value.slice(0, 6) + '-' + value.slice(6);
        } else {
            formattedValue = value.slice(0, 6) + '-' + value.slice(6, 8) + '-' + value.slice(8, 12);
        }

        // Update the input field with the formatted value
        e.target.value = formattedValue;
    });

    const checkboxes = document.querySelectorAll(".seat-checkbox");
    const proceedButton = document.getElementById("proceed-btn");

    checkboxes.forEach((checkbox) => {
        checkbox.addEventListener("change", updateSeatCount);
    });

    function updateSeatCount() {
        const selectedSeats = document.querySelectorAll(".seat-checkbox:checked").length;
        proceedButton.innerText = `Proceed (${selectedSeats} selected)`;
    }


    const $changeProfileText = $(".text_change_profile_pic");
    const $modalOverlay = $("#imageUpdateModal");
    const $closeModalBtn = $("#modalCloseImageBtn");

    // Verify DOM elements
    console.log("Change Profile Text Found:", $changeProfileText.length > 0);
    console.log("Modal Overlay Found:", $modalOverlay.length > 0);

    // Show modal
    $changeProfileText.on("click", function () {
        console.log("Change Profile Picture Clicked");
        $modalOverlay.addClass("active");
    });

    // Close modal on button click
    $closeModalBtn.on("click", function () {
        console.log("Close Button Clicked");
        $modalOverlay.removeClass("active");
    });

    // Close modal when clicking outside modal content
    $modalOverlay.on("click", function (e) {
        if ($(e.target).is($modalOverlay)) {
            console.log("Overlay Clicked");
            $modalOverlay.removeClass("active");
        }
    });

    // Photo preview

    // Photo preview
    $('.upload input').on('change', e => {
        const f = e.target.files[0];
        const img = $(e.target).siblings('img')[0];

        img.dataset.src ??= img.src;

        if (f && f.type.startsWith('image/')) {
            img.onload = e => URL.revokeObjectURL(img.src);
            img.src = URL.createObjectURL(f);
        }
        else {
            img.src = img.dataset.src;
            e.target.value = '';
        }

        // Trigger input validation
        $(e.target).valid();
    });


    // Handle drag-and-drop functionality
    let dropArea = $('#dropArea');
    let fileInput = $('#Photo');

    dropArea.on('dragenter dragover', function (event) {
        event.preventDefault(); // Prevent default drag behavior
        event.stopPropagation();
        dropArea.addClass('dragover'); // Add a class when dragging over
    });

    dropArea.on('dragleave dragend drop', function (event) {
        event.preventDefault();
        event.stopPropagation();
        dropArea.removeClass('dragover'); // Remove class when drag ends or file is dropped
    });

    dropArea.on('drop', function (event) {
        let files = event.originalEvent.dataTransfer.files; // Get the dropped files
        fileInput[0].files = files; // Set the files in the input
        fileInput.trigger('change'); // Trigger the change event to preview the image
    });

    // Handle click event on drop area to open file input dialog
    dropArea.on('click', function () {
        fileInput.click();
    });




});


