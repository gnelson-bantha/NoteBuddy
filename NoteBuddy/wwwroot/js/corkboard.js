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

        // Clamp to prevent dragging off the top-left edge
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
    }
};
