/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./Pages/**/*.cshtml",
        "./Pages/**/**/*.cshtml",
        "./wwwroot/js/**/*.js"
    ],
    theme: {
        extend: {
            colors: {
                subblue: {
                    50: "#eef6ff",
                    100: "#d9ebff",
                    200: "#b6d7ff",
                    300: "#85beff",
                    400: "#4b9cff",
                    500: "#1f7cff",
                    600: "#0060B0",
                    700: "#004a8a",
                    800: "#003b6f",
                    900: "#00325f"
                },
                subgreen: {
                    50: "#effbf1",
                    100: "#d9f5de",
                    200: "#b6eac0",
                    300: "#84d999",
                    400: "#4bc56c",
                    500: "#28a84d",
                    600: "#209030",
                    700: "#177027",
                    800: "#145923",
                    900: "#114a1f"
                }
            },
            animation: {
                "fade-in-up": "fade-in-up 0.6s ease-out forwards",
                "fade-in": "fade-in 0.5s ease-out forwards",
                "float": "float 6s ease-in-out infinite",
                "glow-pulse": "glow-pulse 3s ease-in-out infinite",
                "slide-in-right": "slide-in-right 0.5s ease-out forwards",
            },
            keyframes: {
                "fade-in-up": {
                    "0%": { opacity: "0", transform: "translateY(20px)" },
                    "100%": { opacity: "1", transform: "translateY(0)" },
                },
                "fade-in": {
                    "0%": { opacity: "0" },
                    "100%": { opacity: "1" },
                },
                "float": {
                    "0%, 100%": { transform: "translateY(0)" },
                    "50%": { transform: "translateY(-12px)" },
                },
                "glow-pulse": {
                    "0%, 100%": { opacity: "0.5" },
                    "50%": { opacity: "0.8" },
                },
                "slide-in-right": {
                    "0%": { opacity: "0", transform: "translateX(20px)" },
                    "100%": { opacity: "1", transform: "translateX(0)" },
                },
            },
        }
    },
    plugins: [],
};
