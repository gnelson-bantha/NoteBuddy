/**
 * Corkboard drag-and-drop interop module.
 * Provides pointer-event-based dragging for sticky notes and pinned pictures,
 * with position callbacks to the Blazor server via DotNetObjectReference.
 */
window.corkboardInterop = {
    /** Tracks the currently active drag operation, or null when idle. */
    _activeDrag: null,

    /**
     * Initializes drag-and-drop for a corkboard element.
     * @param {string} elementId - The DOM id of the draggable element (note or picture).
     * @param {string} handleId - The DOM id of the drag handle button.
     * @param {object} dotNetRef - A DotNetObjectReference for invoking OnPositionChanged on the server.
     */
    initDraggable: function (elementId, handleId, dotNetRef) {
        const handle = document.getElementById(handleId);
        if (!handle) return;

        const onPointerDown = (e) => {
            e.preventDefault();
            e.stopPropagation();

            const element = document.getElementById(elementId);
            if (!element) return;

            // Calculate the offset between the pointer and the element's top-left corner
            const rect = element.getBoundingClientRect();
            const scrollContainer = element.closest('.corkboard');
            const scrollLeft = scrollContainer ? scrollContainer.scrollLeft : 0;
            const scrollTop = scrollContainer ? scrollContainer.scrollTop : 0;

            // Store drag state for use in move/up handlers
            window.corkboardInterop._activeDrag = {
                element: element,
                dotNetRef: dotNetRef,
                offsetX: e.clientX - rect.left,
                offsetY: e.clientY - rect.top,
                scrollContainer: scrollContainer
            };

            element.classList.add('dragging');
            document.addEventListener('pointermove', window.corkboardInterop._onPointerMove);
            document.addEventListener('pointerup', window.corkboardInterop._onPointerUp);
        };

        handle.addEventListener('pointerdown', onPointerDown);
    },

    /**
     * Handles pointer move during an active drag, updating the element's position.
     * Accounts for scroll offset within the corkboard container.
     */
    _onPointerMove: function (e) {
        const drag = window.corkboardInterop._activeDrag;
        if (!drag) return;

        e.preventDefault();

        // Recalculate scroll offset on each move to handle mid-drag scrolling
        const scrollContainer = drag.element.closest('.corkboard');
        const scrollLeft = scrollContainer ? scrollContainer.scrollLeft : 0;
        const scrollTop = scrollContainer ? scrollContainer.scrollTop : 0;

        // Compute new position relative to the corkboard container
        let newX = e.clientX + scrollLeft - drag.offsetX - (scrollContainer ? scrollContainer.getBoundingClientRect().left : 0);
        let newY = e.clientY + scrollTop - drag.offsetY - (scrollContainer ? scrollContainer.getBoundingClientRect().top : 0);

        // Clamp to prevent dragging off the top or left edge
        newX = Math.max(0, newX);
        newY = Math.max(0, newY);

        drag.element.style.left = newX + 'px';
        drag.element.style.top = newY + 'px';
    },

    /**
     * Handles pointer up to finalize a drag operation.
     * Removes event listeners, reads the final position, and notifies the Blazor server.
     */
    _onPointerUp: function (e) {
        const drag = window.corkboardInterop._activeDrag;
        if (!drag) return;

        drag.element.classList.remove('dragging');
        document.removeEventListener('pointermove', window.corkboardInterop._onPointerMove);
        document.removeEventListener('pointerup', window.corkboardInterop._onPointerUp);

        // Read the final pixel position and send it back to the server for persistence
        const newX = parseFloat(drag.element.style.left) || 0;
        const newY = parseFloat(drag.element.style.top) || 0;

        drag.dotNetRef.invokeMethodAsync('OnPositionChanged', newX, newY);
        window.corkboardInterop._activeDrag = null;
    },

    /** Tracks the currently active resize operation, or null when idle. */
    _activeResize: null,

    /**
     * Initializes aspect-ratio-locked resizing for a sticky note.
     * @param {string} elementId - The DOM id of the resizable element.
     * @param {string} handleId - The DOM id of the resize handle element.
     * @param {object} dotNetRef - A DotNetObjectReference for invoking OnScaleChanged on the server.
     * @param {number} baseWidth - The base width at scale 1.0 (325px).
     * @param {number} baseHeight - The base height at scale 1.0 (324px).
     * @param {number} minScale - Minimum scale factor (1.0).
     * @param {number} maxScale - Maximum scale factor (2.0).
     */
    initResizable: function (elementId, handleId, dotNetRef, baseWidth, baseHeight, minScale, maxScale) {
        const handle = document.getElementById(handleId);
        if (!handle) return;

        const onPointerDown = (e) => {
            e.preventDefault();
            e.stopPropagation();

            const element = document.getElementById(elementId);
            if (!element) return;

            // Read the current width to determine the starting scale
            const currentWidth = parseFloat(element.style.width) || baseWidth;
            const currentScale = currentWidth / baseWidth;

            window.corkboardInterop._activeResize = {
                element: element,
                dotNetRef: dotNetRef,
                startX: e.clientX,
                startScale: currentScale,
                baseWidth: baseWidth,
                baseHeight: baseHeight,
                minScale: minScale,
                maxScale: maxScale
            };

            element.classList.add('resizing');
            document.addEventListener('pointermove', window.corkboardInterop._onResizeMove);
            document.addEventListener('pointerup', window.corkboardInterop._onResizeUp);
        };

        handle.addEventListener('pointerdown', onPointerDown);
    },

    /**
     * Handles pointer move during a resize, scaling width and height proportionally.
     * Scale is derived from horizontal drag distance relative to base width.
     */
    _onResizeMove: function (e) {
        const resize = window.corkboardInterop._activeResize;
        if (!resize) return;

        e.preventDefault();

        // Calculate new scale from horizontal drag delta
        const deltaX = e.clientX - resize.startX;
        const scaleDelta = deltaX / resize.baseWidth;
        let newScale = resize.startScale + scaleDelta;

        // Clamp scale within bounds
        newScale = Math.max(resize.minScale, Math.min(resize.maxScale, newScale));

        // Apply proportional dimensions and matching background size
        const newWidth = resize.baseWidth * newScale;
        const newHeight = resize.baseHeight * newScale;
        resize.element.style.width = newWidth + 'px';
        resize.element.style.height = newHeight + 'px';
        resize.element.style.backgroundSize = newWidth + 'px ' + newHeight + 'px';
    },

    /**
     * Handles pointer up to finalize a resize operation.
     * Reads the final scale and notifies the Blazor server.
     */
    _onResizeUp: function (e) {
        const resize = window.corkboardInterop._activeResize;
        if (!resize) return;

        resize.element.classList.remove('resizing');
        document.removeEventListener('pointermove', window.corkboardInterop._onResizeMove);
        document.removeEventListener('pointerup', window.corkboardInterop._onResizeUp);

        // Calculate final scale from the element's current width
        const finalWidth = parseFloat(resize.element.style.width) || resize.baseWidth;
        const finalScale = finalWidth / resize.baseWidth;

        resize.dotNetRef.invokeMethodAsync('OnScaleChanged', finalScale);
        window.corkboardInterop._activeResize = null;
    },

    /** Stored reference for the keyboard shortcut handler, used for cleanup. */
    _keydownHandler: null,

    /** Stored reference for the page-level DotNetObjectReference. */
    _pageRef: null,

    /**
     * Registers page-level event listeners for keyboard shortcuts and drag-and-drop.
     * @param {object} dotNetRef - A DotNetObjectReference for invoking page-level callbacks.
     */
    registerPageEvents: function (dotNetRef) {
        window.corkboardInterop._pageRef = dotNetRef;

        // Keyboard shortcut: Alt+N to add a new note
        window.corkboardInterop._keydownHandler = function (e) {
            if (e.altKey && e.key === 'n') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('OnKeyboardNewNote');
            }
        };
        document.addEventListener('keydown', window.corkboardInterop._keydownHandler);

        // Drag-and-drop image onto corkboard
        const corkboard = document.querySelector('.corkboard');
        if (corkboard) {
            corkboard.addEventListener('dragover', window.corkboardInterop._onDragOver);
            corkboard.addEventListener('dragleave', window.corkboardInterop._onDragLeave);
            corkboard.addEventListener('drop', window.corkboardInterop._onDrop);
        }
    },

    /**
     * Unregisters page-level event listeners for keyboard shortcuts and drag-and-drop.
     */
    unregisterPageEvents: function () {
        if (window.corkboardInterop._keydownHandler) {
            document.removeEventListener('keydown', window.corkboardInterop._keydownHandler);
            window.corkboardInterop._keydownHandler = null;
        }

        const corkboard = document.querySelector('.corkboard');
        if (corkboard) {
            corkboard.removeEventListener('dragover', window.corkboardInterop._onDragOver);
            corkboard.removeEventListener('dragleave', window.corkboardInterop._onDragLeave);
            corkboard.removeEventListener('drop', window.corkboardInterop._onDrop);
        }

        window.corkboardInterop._pageRef = null;
    },

    /** Allows drop by preventing default dragover behavior and adding visual feedback. */
    _onDragOver: function (e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'copy';
        e.currentTarget.classList.add('drag-over');
    },

    /** Removes visual feedback when dragging leaves the corkboard. */
    _onDragLeave: function (e) {
        e.currentTarget.classList.remove('drag-over');
    },

    /**
     * Handles file drop on the corkboard. Reads the first image file as base64
     * and sends it to Blazor with the drop coordinates.
     */
    _onDrop: function (e) {
        e.preventDefault();
        e.currentTarget.classList.remove('drag-over');

        const pageRef = window.corkboardInterop._pageRef;
        if (!pageRef) return;

        const files = e.dataTransfer.files;
        if (!files || files.length === 0) return;

        const file = files[0];
        if (!file.type.startsWith('image/')) return;

        // Calculate drop position relative to the corkboard
        const corkboard = e.currentTarget;
        const rect = corkboard.getBoundingClientRect();
        const x = e.clientX - rect.left + corkboard.scrollLeft;
        const y = e.clientY - rect.top + corkboard.scrollTop;

        const reader = new FileReader();
        reader.onload = function () {
            // reader.result is "data:image/png;base64,..." — extract the base64 part
            const base64 = reader.result.split(',')[1];
            const extension = file.name.substring(file.name.lastIndexOf('.')) || '.png';
            pageRef.invokeMethodAsync('OnImageDropped', base64, extension, x, y);
        };
        reader.readAsDataURL(file);
    },

    /**
     * Gets the corkboard-relative position of a mouse event.
     * Returns null if the click was on a sticky note or picture (not empty corkboard space).
     * @param {number} clientX - The client X coordinate.
     * @param {number} clientY - The client Y coordinate.
     * @returns {{x: number, y: number} | null} Position relative to the corkboard, or null.
     */
    getCorkboardClickPosition: function (clientX, clientY) {
        const corkboard = document.querySelector('.corkboard');
        if (!corkboard) return null;

        // Check if the click target is inside a sticky note or picture
        const target = document.elementFromPoint(clientX, clientY);
        if (target && (target.closest('.sticky-note') || target.closest('.pinned-picture') || target.closest('.corkboard-toolbar'))) {
            return null;
        }

        const rect = corkboard.getBoundingClientRect();
        return {
            x: clientX - rect.left + corkboard.scrollLeft,
            y: clientY - rect.top + corkboard.scrollTop
        };
    }
};
