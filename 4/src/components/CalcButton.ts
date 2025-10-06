type ButtonVariant = "neutral" | "action" | "danger" | "accent" | "custom";

class CalcButton extends HTMLElement {
  static get observedAttributes() {
    return [
      "label",
      "action",
      "value",
      "variant",
      "from-color",
      "to-color",
      "width",
      "height",
      "text-size",
      "margin-x",
      "margin-y",
      "text-margin-x",
      "text-margin-y",
      "class",
    ];
  }

  private button: HTMLButtonElement | null = null;
  private span: HTMLSpanElement | null = null;

  connectedCallback() {
    this.render();
  }

  attributeChangedCallback() {
    this.render();
  }

  private getAttr(name: string, fallback?: string) {
    return this.getAttribute(name) ?? fallback;
  }

  private getVariantClasses(variant: ButtonVariant) {
    switch (variant) {
      case "danger":
        return {
          outer: "bg-gradient-to-b from-red-700 to-red-500 hover:to-red-600",
          inner:
            "bg-gradient-to-b from-red-700 to-red-500 group-hover:to-red-600",
          text: "bg-neutral-800 bg-gradient-to-b from-neutral-50/25 to-neutral-950/25",
        };
      case "accent":
        return {
          outer:
            "bg-gradient-to-b from-orange-700 to-orange-500 hover:to-orange-600",
          inner:
            "bg-gradient-to-b from-orange-700 to-orange-500 group-hover:to-orange-600",
          text: "bg-neutral-800 bg-gradient-to-b from-neutral-50/25 to-neutral-950/25",
        };
      case "neutral":
        return {
          outer:
            "bg-gradient-to-b from-neutral-400 to-neutral-200 hover:to-neutral-300",
          inner:
            "bg-gradient-to-b from-neutral-400 to-neutral-200 group-hover:to-neutral-300",
          text: "bg-neutral-800 bg-gradient-to-b from-neutral-50/25 to-neutral-950/25",
        };
      case "action":
        return {
          outer:
            "bg-gradient-to-b from-neutral-700 to-neutral-500 hover:to-neutral-600",
          inner:
            "bg-gradient-to-b from-neutral-700 to-neutral-500 group-hover:to-neutral-600",
          text: "bg-neutral-50 bg-gradient-to-b from-neutral-50/25 to-neutral-950/25",
        };
      case "custom":
        const fromColor = this.getAttr("from-color", "neutral-400");
        const toColor = this.getAttr("to-color", "neutral-200");
        return {
          outer: `bg-gradient-to-b from-${fromColor} to-${toColor}`,
          inner: `bg-gradient-to-b from-${fromColor} to-${toColor}`,
          text: "bg-neutral-800 bg-gradient-to-b from-neutral-50/25 to-neutral-950/25",
        };
    }
  }

  private render() {
    const label = this.getAttr("label", "");
    const action = this.getAttr("action");
    const value = this.getAttr("value");
    const variant =
      (this.getAttr("variant", "neutral") as ButtonVariant) || "neutral";

    const width = this.getAttr("width", "4rem");
    const height = this.getAttr("height", "4rem");
    const textSize = this.getAttr("text-size", "text-5xl");

    const marginX = this.getAttr("margin-x", "0");
    const marginY = this.getAttr("margin-y", "0");
    const textMarginX = this.getAttr("text-margin-x", "0");
    const textMarginY = this.getAttr("text-margin-y", "0");

    const extraClass = this.getAttr("class", "");

    const variantClasses = this.getVariantClasses(variant);

    this.innerHTML = `
      <button
        class="rounded-lg p-[1px] shadow-button hover:shadow-button-close active:shadow-button-press active:scale-95 transition-all duration-75 ease-out group ${
          variantClasses.outer
        } ${extraClass}"
        style="width:${width};height:${height};margin:${marginY} ${marginX};"
        ${action ? `data-action="${action}"` : ""}
        ${value ? `data-value="${value}"` : ""}
      >
        <span class="size-full ${
          variantClasses.inner
        } rounded-lg relative btn-highlight overflow-clip flex items-center justify-center group-active:brightness-90 transition-colors duration-75 ease-out">
          <span class="btn-label bg-clip-text ${
            variantClasses.text
          } text-transparent font-sourcesans ${textSize}"
          style="transform: translate(${textMarginX}, ${textMarginY}); display: inline-block;"
        >
            ${label}
          </span>
        </span>
      </button>
    `;

    this.button = this.querySelector("button");
    this.span = this.querySelector(".btn-label");
  }
}

customElements.define("calc-button", CalcButton);
export {};
