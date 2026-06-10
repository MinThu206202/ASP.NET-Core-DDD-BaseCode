@model UserApp.Web.ViewModels.{{Name}}ViewModel

<h2>Edit {{Name}}</h2>

<form asp-action="Edit" method="post" enctype="multipart/form-data">

    @Html.AntiForgeryToken()

    <input type="hidden" asp-for="Id" />

    <div asp-validation-summary="ModelOnly"
         class="text-danger mb-3">
    </div>

{{Inputs}}

    <div class="mt-3">
        <button type="submit"
                class="btn btn-primary">
            Update
        </button>

        <a asp-action="Index"
           class="btn btn-secondary">
            Back
        </a>
    </div>

</form>

@section Scripts
{
    <partial name="_ValidationScriptsPartial" />
}