﻿using Groupify.ViewModels.Group;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Groupify.ViewModels.Room
{
    public class CompositeRoomViewModel
    {
        [ValidateNever]
        public DetailsRoomViewModel RoomDetails { get; set; } = new DetailsRoomViewModel();

        public CreateGroupViewModel CreateGroup { get; set; } = new CreateGroupViewModel();
    }
}