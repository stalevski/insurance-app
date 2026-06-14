// Theme interop: persists the light/dark preference in localStorage and
// applies it to the document element so CSS variables switch themes.
window.themeInterop = {
    storageKey: "ii-theme",

    get: function () {
        return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
    },

    set: function (theme) {
        const next = theme === "dark" ? "dark" : "light";
        document.documentElement.setAttribute("data-theme", next);
        try {
            localStorage.setItem(this.storageKey, next);
        } catch (e) {
            // Ignore storage failures (e.g. private mode); theme still applies for this session.
        }
        // Mermaid bakes the theme into the SVG at render time, so re-render any
        // existing diagrams to match the new light/dark theme.
        if (window.mermaidInterop && typeof window.mermaidInterop.rerenderAll === "function") {
            window.mermaidInterop.rerenderAll();
        }
        return next;
    },

    toggle: function () {
        return this.set(this.get() === "dark" ? "light" : "dark");
    }
};

// Mermaid interop for the domain-event flow diagram.
window.mermaidInterop = {
    initializedTheme: null,

    // Remember each rendered diagram so it can be re-rendered when the light/dark
    // theme changes (Mermaid bakes the theme into the SVG at render time).
    diagrams: new Map(),

    // Resolve once the Mermaid script has finished loading. The component's
    // OnAfterRenderAsync can fire before the script is ready, so we poll
    // briefly instead of failing (and leaving a stale error graphic on) the first render.
    waitForMermaid: function (timeoutMs) {
        return new Promise((resolve) => {
            if (typeof mermaid !== "undefined") {
                resolve(true);
                return;
            }

            const started = Date.now();
            const timer = setInterval(() => {
                if (typeof mermaid !== "undefined") {
                    clearInterval(timer);
                    resolve(true);
                } else if (Date.now() - started > timeoutMs) {
                    clearInterval(timer);
                    resolve(false);
                }
            }, 50);
        });
    },

    // Mermaid's "dark" theme renders light text and edges for a dark surface;
    // "neutral" suits the light theme. Mirror the document's data-theme.
    themeForDocument: function () {
        return document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "neutral";
    },

    ensureInit: function () {
        if (typeof mermaid === "undefined") {
            return;
        }

        const theme = this.themeForDocument();
        if (this.initializedTheme === theme) {
            return;
        }

        // Match the diagram canvas to the surrounding panel so there is no bright
        // rectangle in dark mode.
        const surface = getComputedStyle(document.documentElement)
            .getPropertyValue("--panel-muted").trim();
        mermaid.initialize({
            startOnLoad: false,
            securityLevel: "strict",
            theme: theme,
            themeVariables: surface ? { background: surface } : {}
        });
        this.initializedTheme = theme;
    },

    renderInto: async function (elementId, definition) {
        try {
            const { svg } = await mermaid.render(elementId + "-svg", definition);
            const element = document.getElementById(elementId);
            if (element) {
                element.innerHTML = svg;
            }
        } catch (err) {
            const element = document.getElementById(elementId);
            if (element) {
                element.innerHTML = "<p class='muted'>Unable to render flow diagram.</p>";
            }
            console.error("mermaid render failed", err);
        }
    },

    render: async function (elementId, definition) {
        const element = document.getElementById(elementId);
        if (!element) {
            return;
        }

        if (!definition) {
            element.innerHTML = "";
            this.diagrams.delete(elementId);
            return;
        }

        const ready = await this.waitForMermaid(5000);
        if (!ready) {
            element.innerHTML = "<p class='muted'>Diagram library failed to load.</p>";
            return;
        }

        this.diagrams.set(elementId, definition);
        this.ensureInit();
        await this.renderInto(elementId, definition);
    },

    // Re-render every cached diagram (e.g. after the theme changes) so existing
    // diagrams pick up the new Mermaid theme.
    rerenderAll: async function () {
        if (this.diagrams.size === 0) {
            return;
        }

        const ready = await this.waitForMermaid(5000);
        if (!ready) {
            return;
        }

        this.ensureInit();
        for (const [elementId, definition] of this.diagrams) {
            if (document.getElementById(elementId)) {
                await this.renderInto(elementId, definition);
            } else {
                this.diagrams.delete(elementId);
            }
        }
    }
};
